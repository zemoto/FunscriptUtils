using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VlcScriptPlayer.Handy.Api;

internal enum ConnectionStatus
{
   Connected,
   FailedToConnect,
   DeviceIncompatible
}

internal abstract class HandyApiBase
{
   protected readonly HttpClient _client;

   private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
   private readonly Endpoints _endpoints;

   private long _estimatedClientServerOffset;
   private string _lastUploadedScriptSha256;

   protected HandyApiBase( HttpClient client )
   {
      _client = client;
      _endpoints = GetEndpoints();

      var retryStrategy = new RetryStrategyOptions<HttpResponseMessage>
      {
         Delay = TimeSpan.FromSeconds( 1 ),
         OnRetry = static args =>
         {
            Logger.LogError( $"Exception on attempt #{args.AttemptNumber}: {args.Outcome.Exception.Message}" );
            return default;
         }
      };
      _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().AddRetry( retryStrategy ).Build();
   }

   public async Task<ConnectionStatus> ConnectToAndSetupHandyAsync( string connectionId )
   {
      _client.DefaultRequestHeaders.Clear();
      _lastUploadedScriptSha256 = string.Empty;
      _estimatedClientServerOffset = 0;

      var headers = await GetClientHeadersAsync( connectionId );
      if ( headers is null )
      {
         return ConnectionStatus.FailedToConnect;
      }

      _client.DefaultRequestHeaders.Add( "X-Connection-Key", connectionId );
      foreach ( var header in headers )
      {
         _client.DefaultRequestHeaders.Add( header.Key, header.Value );
      }

      if ( !await ConnectAsync() )
      {
         return ConnectionStatus.FailedToConnect;
      }

      if ( !await GetDeviceCompatibleAsync() )
      {
         return ConnectionStatus.DeviceIncompatible;
      }

      if ( !await SetupServerClockSyncAsync() || !await EnsureModeAsync() )
      {
         return ConnectionStatus.FailedToConnect;
      }

      return ConnectionStatus.Connected;
   }

   public async Task SetOffsetAsync( int offset )
   {
      var content = new StringContent( $"{{ \"offset\": {offset} }}", Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( _endpoints.OffsetEndpoint, content ), "SetOffset", $"Offset set to {offset}ms" );
   }

   public async Task<(double, double)> GetRangeAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( _endpoints.SlideEndpoint ) );
      if ( response?.IsSuccessStatusCode != true )
      {
         return (0, 0);
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var slideResponse = JsonSerializer.Deserialize<GetSlideResponse>( responseString );

      return (slideResponse.Min, slideResponse.Max);
   }

   public async Task SetRangeAsync( double min, double max )
   {
      if ( min >= max - 10 )
      {
         Logger.LogError( "Invalid slide min/max range" );
         return;
      }

      var content = new StringContent( $"{{ \"min\": {min}, \"max\": {max} }}", Encoding.UTF8, "application/json" );
      using var request = await DoRequest( () => _client.PutAsync( _endpoints.SlideEndpoint, content ), "SetRange", $"Range set to {min}-{max}" );
   }

   public async Task<bool> UploadScriptAsync( Funscript script )
   {
      Logger.Log( "Retrieving script CSV" );
      var csv = script.GetCSV();
      if ( string.IsNullOrEmpty( csv ) )
      {
         Logger.LogError( "Invalid script" );
         return false;
      }

      var csvSha256Hash = ComputeSha256Hash( csv );
      if ( csvSha256Hash == _lastUploadedScriptSha256 )
      {
         Logger.Log( "Script is identical to last uploaded, skipping upload" );
         return true;
      }

      var formData = new MultipartFormDataContent { { new StringContent( csv ), "syncFile", "VlcScriptPlayer.csv" } };

      using var uploadResponse = await DoRequest( () => _client.PostAsync( Endpoints.UploadCSVEndpoint, formData ), "UploadingScript" );
      if ( uploadResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await uploadResponse.Content.ReadAsStringAsync();
      var parsedUploadResponse = JsonSerializer.Deserialize<UploadResponse>( responseString );
      if ( !parsedUploadResponse.Success )
      {
         Logger.LogError( parsedUploadResponse.Info );
         return false;
      }

      var setupContent = new StringContent( $"{{ \"url\": \"{parsedUploadResponse.Url}\" }}", Encoding.UTF8, "application/json" );
      using var setupResponse = await DoRequest( () => _client.PutAsync( _endpoints.SetupEndpoint, setupContent ), "SyncSetup" );
      if ( setupResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      responseString = await setupResponse.Content.ReadAsStringAsync();
      if ( !SetupSuccessful( responseString, out string error ) )
      {
         Logger.LogError( error );
         return false;
      }

      _lastUploadedScriptSha256 = csvSha256Hash;
      return true;
   }

   public async Task PlayScriptAsync( long startTime )
   {
      var estimatedServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      using var _ = await DoRequest( () => _client.PutAsync( _endpoints.PlayEndpoint, GetPlayScriptContent( estimatedServerTime, startTime ) ) );
   }

   public async Task StopScriptAsync()
   {
      using var _ = await DoRequest( () => _client.PutAsync( _endpoints.StopEndpoint, null ) );
   }

   public async Task SyncTimeAsync( long currentTime )
   {
      var estimatedServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      var syncContent = new StringContent( $"{{ \"current_time\": {currentTime}, \"server_time\": {estimatedServerTime}, \"filter\": 0.5 }}", Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( _endpoints.SyncTimeEndpoint, syncContent ) );
   }

   protected virtual bool CanSyncTime { get; }
   protected abstract Endpoints GetEndpoints();
   protected abstract Task<bool> GetDeviceCompatibleAsync();
   protected abstract Task<Dictionary<string, string>> GetClientHeadersAsync( string connectionId );
   protected abstract bool ConnectionSuccessful( string responseString, out string error );
   protected abstract bool SetupSuccessful( string responseString, out string error );
   protected abstract long ParseServerTimeResponse( string responseString );
   protected abstract StringContent GetPlayScriptContent( long serverTime, long startTime );

   protected async Task<HttpResponseMessage> DoRequest( Func<Task<HttpResponseMessage>> request, string requestName, string successMessage = "" )
   {
      try
      {
         Logger.Log( $"Sending Request: {requestName}" );
         var response = await _pipeline.ExecuteAsync( async _ => await request(), CancellationToken.None );
         if ( !response.IsSuccessStatusCode )
         {
            Logger.LogError( $"Request failed with code {response.StatusCode}" );
         }
         else if ( !string.IsNullOrEmpty( successMessage ) )
         {
            Logger.Log( successMessage );
         }

         return response;
      }
      catch
      {
         Logger.LogError( "Request failed" );
         return null;
      }
   }

   private async Task<bool> ConnectAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( _endpoints.CheckConnectionEndpoint ), "Connect" );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var connected = ConnectionSuccessful( responseString, out string error );
      if ( connected )
      {
         Logger.Log( "Connection successful" );
      }
      else if ( !string.IsNullOrEmpty( error ) )
      {
         Logger.LogError( error );
      }
      else
      {
         Logger.LogError( "No Handy found to connect to" );
      }

      return connected;
   }

   private async Task<bool> SetupServerClockSyncAsync()
   {
      Logger.Log( "Syncing clock with server clock" );
      var calculatedOffsets = new List<double>();
      for ( int i = 0; i < 15; i++ )
      {
         var clientSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
         using var response = await DoRequest( () => _client.GetAsync( _endpoints.ServerClockEndpoint ) );
         var clientReceiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
         if ( response?.IsSuccessStatusCode != true )
         {
            return false;
         }

         var serverTimeRawResponse = await response.Content.ReadAsStringAsync();
         var serverReceiveTime = ParseServerTimeResponse( serverTimeRawResponse );

         var rtd = clientReceiveTime - clientSendTime;
         var clientReceiveServerTime = serverReceiveTime + ( rtd / 2 );

         calculatedOffsets.Add( clientReceiveServerTime - clientReceiveTime );
      }

      var mean = calculatedOffsets.Average();
      var sd = Math.Sqrt( calculatedOffsets.Average( offset => Math.Pow( offset - mean, 2 ) ) );
      _estimatedClientServerOffset = (long)calculatedOffsets.Where( offset => Math.Abs( offset - mean ) < sd ).Average();
      Logger.Log( $"Server clock sync completed: {_estimatedClientServerOffset}ms offset" );

      return true;
   }

   private async Task<bool> EnsureModeAsync()
   {
      var content = new StringContent( "{ \"mode\": 1 }", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( () => _client.PutAsync( _endpoints.ModeEndpoint, content ), "SetMode", "Mode set to HSSP" );
      return response?.IsSuccessStatusCode == true;
   }

   private async Task<HttpResponseMessage> DoRequest( Func<Task<HttpResponseMessage>> request )
   {
      try
      {
         return await _pipeline.ExecuteAsync( async _ => await request(), CancellationToken.None );
      }
      catch
      {
         return null;
      }
   }

   private static string ComputeSha256Hash( string rawData )
   {
      byte[] bytes = System.Security.Cryptography.SHA256.HashData( Encoding.UTF8.GetBytes( rawData ) );

      var sb = new StringBuilder();
      for ( int i = 0; i < bytes.Length; i++ )
      {
         _ = sb.Append( bytes[i].ToString( "x2", CultureInfo.InvariantCulture ) );
      }
      return sb.ToString();
   }

   protected sealed class Endpoints( string root, string slideEndpoint )
   {
      public string CheckConnectionEndpoint = $"{root}connected";
      public string ServerClockEndpoint = $"{root}servertime";
      public string ModeEndpoint = $"{root}mode";
      public string OffsetEndpoint = $"{root}hstp/offset";
      public string SetupEndpoint = $"{root}hssp/setup";
      public string PlayEndpoint = $"{root}hssp/play";
      public string StopEndpoint = $"{root}hssp/stop";
      public string SyncTimeEndpoint = $"{root}hssp/synctime";
      public string SlideEndpoint = slideEndpoint;

      public const string UploadCSVEndpoint = "https://www.handyfeeling.com/api/sync/upload?local=true";
   }
}