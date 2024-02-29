using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyApi : IDisposable
{
   private readonly HttpClient _client = new();
   private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

   private long _estimatedClientServerOffset;
   private string _lastUploadedScriptSha256;

   public HandyApi()
   {
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

   public void Dispose() => _client.Dispose();

   public async Task<bool> ConnectToAndSetupHandyAsync( string connectionId )
   {
      _ = _client.DefaultRequestHeaders.Remove( "X-Connection-Key" );
      _client.DefaultRequestHeaders.Add( "X-Connection-Key", connectionId );

      return await ConnectAsync() && await SetupServerClockSyncAsync() && await EnsureModeAsync();
   }

   private async Task<bool> ConnectAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( Endpoints.CheckConnectionEndpoint ), "Connect" );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var parsedResponse = JsonSerializer.Deserialize<ConnectedResponse>( responseString );

      if ( parsedResponse.IsConnected )
      {
         Logger.Log( "Connection successful" );
      }
      else
      {
         Logger.LogError( "No Handy found to connect to" );
      }

      return parsedResponse.IsConnected;
   }

   private async Task<bool> SetupServerClockSyncAsync()
   {
      Logger.Log( "Syncing clock with server clock" );
      var calculatedOffsets = new List<double>();
      for ( int i = 0; i < 30; i++ )
      {
         var clientSendTime = DateTimeOffset.Now;
         using var response = await DoRequest( () => _client.GetAsync( Endpoints.ServerClockEndpoint ) );
         var clientReceiveTime = DateTimeOffset.Now;
         if ( response?.IsSuccessStatusCode != true )
         {
            return false;
         }

         var serverTimeRawResponse = await response.Content.ReadAsStringAsync();
         var serverTimeResponse = JsonSerializer.Deserialize<ServerTimeResponse>( serverTimeRawResponse );

         var rtd = clientReceiveTime - clientSendTime;
         var clientReceiveServerTime = serverTimeResponse.ServerTime + ( rtd / 2 ).TotalMilliseconds;

         calculatedOffsets.Add( clientReceiveServerTime - clientReceiveTime.ToUnixTimeMilliseconds() );
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
      using var response = await DoRequest( () => _client.PutAsync( Endpoints.ModeEndpoint, content ), "SetMode", "Mode set to HSSP" );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task SetOffsetAsync( int offset )
   {
      var content = new StringContent( $"{{ \"offset\": {offset} }}", Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( Endpoints.OffsetEndpoint, content ), "SetOffset", $"Offset set to {offset}ms" );
   }

   public async Task<(double, double)> GetRangeAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( Endpoints.SlideEndpoint ) );
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
      using var request = await DoRequest( () => _client.PutAsync( Endpoints.SlideEndpoint, content ), "SetRange", $"Range set to {min}-{max}" );
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
      using var setupResponse = await DoRequest( () => _client.PutAsync( Endpoints.SetupEndpoint, setupContent ), "SyncSetup" );
      if ( setupResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      responseString = await setupResponse.Content.ReadAsStringAsync();
      var parsedSetupResponse = JsonSerializer.Deserialize<SetupResponse>( responseString );
      if ( parsedSetupResponse.Result == -1 )
      {
         Logger.LogError( parsedSetupResponse.Error.Message );
         return false;
      }

      _lastUploadedScriptSha256 = csvSha256Hash;
      return true;
   }

   public async Task PlayScriptAsync( long startTime )
   {
      var estimatedServerTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      var content = new StringContent( $"{{ \"estimatedServerTime\": {estimatedServerTime}, \"startTime\": {startTime} }}", Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( Endpoints.PlayEndpoint, content ) );
   }

   public async Task StopScriptAsync()
   {
      using var _ = await DoRequest( () => _client.PutAsync( Endpoints.StopEndpoint, null ) );
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

   private async Task<HttpResponseMessage> DoRequest( Func<Task<HttpResponseMessage>> request, string requestName, string successMessage = "" )
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

   private static string ComputeSha256Hash( string rawData )
   {
      byte[] bytes = System.Security.Cryptography.SHA256.HashData( Encoding.UTF8.GetBytes( rawData ) );

      var sb = new StringBuilder();
      for ( int i = 0; i < bytes.Length; i++ )
      {
         _ = sb.Append( bytes[i].ToString( "x2", System.Globalization.CultureInfo.InvariantCulture ) );
      }
      return sb.ToString();
   }
}