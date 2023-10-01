using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VlcScriptPlayer.Config;
using ZemotoCommon;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyApi : IDisposable
{
   private readonly HttpClient _client = new();

   private long _estimatedClientServerOffset;
   private string _lastUploadedScriptSha256;

   public void Dispose() => _client.Dispose();

   public async Task<bool> ConnectToAndSetupHandyAsync( HandyConfig config )
   {
      _client.DefaultRequestHeaders.Remove( "X-Connection-Key" );
      _client.DefaultRequestHeaders.Add( "X-Connection-Key", config.ConnectionId );

      return await ConnectAsync().ConfigureAwait( false ) &&
             await SetupServerClockSyncAsync().ConfigureAwait( false ) &&
             await EnsureModeAsync().ConfigureAwait( false ) &&
             await SetOffsetAsync( config.DesiredOffset ).ConfigureAwait( false ) &&
             await SetRangeAsync( config.DesiredSlideMin, config.DesiredSlideMax ).ConfigureAwait( false );
   }

   private async Task<bool> ConnectAsync()
   {
      Logger.LogRequest( "Connect" );
      using var response = await DoRequest( _client.GetAsync( Endpoints.CheckConnectionEndpoint ) ).ConfigureAwait( false );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
      var parsedResponse = JsonSerializer.Deserialize<ConnectedResponse>( responseString );

      if ( !parsedResponse.IsConnected )
      {
         Logger.Log( "Error: No Handy found to connect to" );
      }

      return parsedResponse.IsConnected;
   }

   private async Task<bool> SetupServerClockSyncAsync()
   {
      Logger.LogRequest( "ServerClock" );
      var calculatedOffsets = new List<double>();
      for ( int i = 0; i < 30; i++ )
      {
         var clientSendTime = DateTimeOffset.Now;
         using var response = await SafeMethod.InvokeSafelyAsync( _client.GetAsync( Endpoints.ServerClockEndpoint ) ).ConfigureAwait( false );
         var clientReceiveTime = DateTimeOffset.Now;
         if ( response?.IsSuccessStatusCode != true )
         {
            Logger.LogRequestFail( response.StatusCode );
            return false;
         }

         var serverTimeRawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
         var serverTimeResponse = JsonSerializer.Deserialize<ServerTimeResponse>( serverTimeRawResponse );

         var rtd = clientReceiveTime - clientSendTime;
         var clientReceiveServerTime = serverTimeResponse.ServerTime + ( rtd / 2 ).TotalMilliseconds;

         calculatedOffsets.Add( clientReceiveServerTime - clientReceiveTime.ToUnixTimeMilliseconds() );
      }

      var mean = calculatedOffsets.Average();
      var sd = Math.Sqrt( calculatedOffsets.Select( offset => Math.Pow( offset - mean, 2 ) ).Average() );
      _estimatedClientServerOffset = (long)calculatedOffsets.Where( offset => Math.Abs( offset - mean ) < sd ).Average();
      Logger.Log( $"Server clock sync completed: {_estimatedClientServerOffset}ms offset" );

      return true;
   }

   private async Task<bool> EnsureModeAsync()
   {
      Logger.LogRequest( "SetMode" );
      var content = new StringContent( "{ \"mode\": 1 }", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( _client.PutAsync( Endpoints.ModeEndpoint, content ) ).ConfigureAwait( false );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task<int> GetOffsetAsync()
   {
      Logger.LogRequest( "GetOffset" );
      using var response = await DoRequest( _client.GetAsync( Endpoints.OffsetEndpoint ) ).ConfigureAwait( false );
      if ( response?.IsSuccessStatusCode != true )
      {
         return 0;
      }

      var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
      var offsetResponse = JsonSerializer.Deserialize<GetOffsetResponse>( responseString );

      return offsetResponse.Offset;
   }

   public async Task<bool> SetOffsetAsync( int offset )
   {
      Logger.LogRequest( "SetOffset" );

      var content = new StringContent( $"{{ \"offset\": {offset} }}", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( _client.PutAsync( Endpoints.OffsetEndpoint, content ) ).ConfigureAwait( false );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task<(double, double)> GetRangeAsync()
   {
      Logger.LogRequest( "GetRange" );
      using var response = await DoRequest( _client.GetAsync( Endpoints.SlideEndpoint ) ).ConfigureAwait( false );
      if ( response?.IsSuccessStatusCode != true )
      {
         return (0, 0);
      }

      var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
      var slideResponse = JsonSerializer.Deserialize<GetSlideResponse>( responseString );

      return (slideResponse.Min, slideResponse.Max);
   }

   public async Task<bool> SetRangeAsync( double min, double max )
   {
      if ( min >= max - 10 )
      {
         Logger.Log( "ERROR: Invalid slide min/max range" );
         return false;
      }

      Logger.LogRequest( "SetRange" );

      var content = new StringContent( $"{{ \"min\": {min}, \"max\": {max} }}", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( _client.PutAsync( Endpoints.SlideEndpoint, content ) ).ConfigureAwait( false );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task<bool> UploadScriptAsync( string scriptFilePath, bool forceUploadScript )
   {
      Logger.Log( "Retrieving script CSV." );
      string csv = CSVFactory.FromFile( scriptFilePath );
      if ( string.IsNullOrEmpty( csv ) )
      {
         Logger.Log( "Error: Invalid script." );
         return true;
      }

      var csvSha256Hash = ComputeSha256Hash( csv );
      if ( !forceUploadScript && csvSha256Hash == _lastUploadedScriptSha256 )
      {
         Logger.Log( "Script is identical to last uploaded, skipping upload." );
         return true;
      }

      var formData = new MultipartFormDataContent { { new StringContent( csv ), "syncFile", $"{Path.GetFileNameWithoutExtension( scriptFilePath )}.csv" } };

      Logger.LogRequest( "UploadingScript" );
      using var uploadResponse = await DoRequest( _client.PostAsync( Endpoints.UploadCSVEndpoint, formData ) ).ConfigureAwait( false );
      if ( uploadResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await uploadResponse.Content.ReadAsStringAsync().ConfigureAwait( false );
      var parsedUploadResponse = JsonSerializer.Deserialize<UploadResponse>( responseString );
      if ( !parsedUploadResponse.Success )
      {
         Logger.Log( $"Upload failed: {parsedUploadResponse.Info}" );
         return false;
      }

      Logger.LogRequest( "SyncSetup" );
      var setupContent = new StringContent( $"{{ \"url\": \"{parsedUploadResponse.Url}\" }}", Encoding.UTF8, "application/json" );
      using var setupResponse = await DoRequest( _client.PutAsync( Endpoints.SetupEndpoint, setupContent ) ).ConfigureAwait( false );
      if ( setupResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      responseString = await setupResponse.Content.ReadAsStringAsync().ConfigureAwait( false );
      var parsedSetupResponse = JsonSerializer.Deserialize<SetupResponse>( responseString );
      if ( parsedSetupResponse.Result == -1 )
      {
         Logger.Log( $"Setup failed: {parsedSetupResponse.Error.Message}" );
         return false;
      }

      _lastUploadedScriptSha256 = csvSha256Hash;
      return true;
   }

   public async Task PlayScriptAsync( long startTime )
   {
      var estimatedServerTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      var content = new StringContent( $"{{ \"estimatedServerTime\": {estimatedServerTime}, \"startTime\": {startTime} }}", Encoding.UTF8, "application/json" );
      using var _ = await SafeMethod.InvokeSafelyAsync( _client.PutAsync( Endpoints.PlayEndpoint, content ) ).ConfigureAwait( false );
   }

   public async Task StopScriptAsync()
   {
      using var _ = await SafeMethod.InvokeSafelyAsync( _client.PutAsync( Endpoints.StopEndpoint, null ) ).ConfigureAwait( false );
   }

   private static async Task<HttpResponseMessage> DoRequest( Task<HttpResponseMessage> request )
   {
      var response = await SafeMethod.InvokeSafelyAsync( request, ex => Logger.Log( $"Exception during request: {ex.Message}" ) ).ConfigureAwait( false );
      if ( response is null )
      {
         return null;
      }

      if ( response.IsSuccessStatusCode )
      {
         Logger.LogRequestSuccess();
      }
      else
      {
         Logger.LogRequestFail( response.StatusCode );
      }

      return response;
   }

   private static string ComputeSha256Hash( string rawData )
   {
      using var hasher = System.Security.Cryptography.SHA256.Create();
      byte[] bytes = hasher.ComputeHash( Encoding.UTF8.GetBytes( rawData ) );

      var sb = new StringBuilder();
      for ( int i = 0; i < bytes.Length; i++ )
      {
         sb.Append( bytes[i].ToString( "x2", System.Globalization.CultureInfo.InvariantCulture ) );
      }
      return sb.ToString();
   }
}