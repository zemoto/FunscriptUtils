using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyApi : IDisposable
{
   private readonly HttpClient _client = new();

   private long _estimatedClientServerOffset;
   private string _lastUploadedScriptSha256;

   public void Dispose() => _client.Dispose();

   public void SetConnectionId( string connectionId )
   {
      _client.DefaultRequestHeaders.Remove( "X-Connection-Key" );
      _client.DefaultRequestHeaders.Add( "X-Connection-Key", connectionId );
   }

   public async Task<bool> ConnectAsync()
   {
      HandyLogger.LogRequest( "Connect" );
      using var response = await DoRequest( _client.GetAsync( Endpoints.CheckConnectionEndpoint ) ).ConfigureAwait( false );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
      var parsedResponse = JsonSerializer.Deserialize<ConnectedResponse>( responseString );

      if ( !parsedResponse.IsConnected )
      {
         HandyLogger.Log( "Error: No Handy found to connect to" );
      }

      return parsedResponse.IsConnected;
   }

   public async Task<bool> SetupServerClockSyncAsync()
   {
      HandyLogger.LogRequest( "ServerClock" );
      var calculatedOffsets = new List<double>();
      for ( int i = 0; i < 30; i++ )
      {
         var clientSendTime = DateTimeOffset.Now;
         using var response = await _client.GetAsync( Endpoints.ServerClockEndpoint ).ConfigureAwait( false );
         var clientReceiveTime = DateTimeOffset.Now;
         if ( response?.IsSuccessStatusCode == true )
         {
            var serverTimeRawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
            var serverTimeResponse = JsonSerializer.Deserialize<ServerTimeResponse>( serverTimeRawResponse );

            var rtd = clientReceiveTime - clientSendTime;
            var clientReceiveServerTime = serverTimeResponse.ServerTime + ( rtd / 2 ).TotalMilliseconds;

            calculatedOffsets.Add( clientReceiveServerTime - clientReceiveTime.ToUnixTimeMilliseconds() );
         }
         else
         {
            HandyLogger.LogRequestFail( response.StatusCode );
            return false;
         }
      }

      var mean = calculatedOffsets.Average();
      var sd = Math.Sqrt( calculatedOffsets.Select( offset => Math.Pow( offset - mean, 2 ) ).Average() );
      var validValues = calculatedOffsets.Where( offset => Math.Abs( offset - mean ) < sd );

      _estimatedClientServerOffset = (long)validValues.Average();
      var unfileteredNonse = calculatedOffsets.Average();
      HandyLogger.Log( $"Server clock sync completed: {_estimatedClientServerOffset}ms offset" );

      return true;
   }

   public async Task<bool> EnsureModeAsync()
   {
      HandyLogger.LogRequest( "SetMode" );
      var content = new StringContent( "{ \"mode\": 1 }", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( _client.PutAsync( Endpoints.ModeEndpoint, content ) ).ConfigureAwait( false );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task<int> GetOffsetAsync()
   {
      HandyLogger.LogRequest( "GetOffset" );
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
      HandyLogger.LogRequest( "SetOffset" );

      var content = new StringContent( $"{{ \"offset\": {offset} }}", Encoding.UTF8, "application/json" );
      using var response = await DoRequest( _client.PutAsync( Endpoints.OffsetEndpoint, content ) ).ConfigureAwait( false );
      return response?.IsSuccessStatusCode == true;
   }

   public async Task<bool> UploadScriptAsync( string scriptFilePath, bool forceUploadScript )
   {
      string csv;
      if ( Path.GetExtension( scriptFilePath ) == ".funscript" )
      {
         HandyLogger.Log( "Converting script to CSV" );
         var funscriptString = File.ReadAllText( scriptFilePath );
         var funscript = JsonSerializer.Deserialize<Funscript>( funscriptString );
         csv = funscript.GetCSVString();
      }
      else
      {
         csv = File.ReadAllText( scriptFilePath );
      }

      var csvSha256Hash = ComputeSha256Hash( csv );
      if ( !forceUploadScript && csvSha256Hash == _lastUploadedScriptSha256 )
      {
         HandyLogger.Log( "Script is identical to last uploaded, skipping upload." );
         return true;
      }
      _lastUploadedScriptSha256 = csvSha256Hash;

      var formData = new MultipartFormDataContent { { new StringContent( csv ), "syncFile", $"{Path.GetFileNameWithoutExtension( scriptFilePath )}.csv" } };

      HandyLogger.LogRequest( "UploadingScript" );
      using var uploadResponse = await DoRequest( _client.PostAsync( Endpoints.UploadCSVEndpoint, formData ) ).ConfigureAwait( false );
      if ( uploadResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await uploadResponse.Content.ReadAsStringAsync().ConfigureAwait( false );
      var parsedUploadResponse = JsonSerializer.Deserialize<UploadResponse>( responseString );
      if ( !parsedUploadResponse.Success )
      {
         HandyLogger.Log( $"Upload failed: {parsedUploadResponse.Info}" );
         return false;
      }

      HandyLogger.LogRequest( "SyncSetup" );
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
         HandyLogger.Log( $"Setup failed: {parsedSetupResponse.Error.Message}" );
         return false;
      }

      return true;
   }

   public async Task PlayScriptAsync( long startTime )
   {
      var estimatedServerTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      var content = new StringContent( $"{{ \"estimatedServerTime\": {estimatedServerTime}, \"startTime\": {startTime} }}", Encoding.UTF8, "application/json" );
      using var _ = await _client.PutAsync( Endpoints.PlayEndpoint, content ).ConfigureAwait( false );
   }

   public async Task StopScriptAsync()
   {
      using var _ = await _client.PutAsync( Endpoints.StopEndpoint, null ).ConfigureAwait( false );
   }

   private static async Task<HttpResponseMessage> DoRequest( Task<HttpResponseMessage> request )
   {
      HttpResponseMessage response;
      try
      {
         response = await request.ConfigureAwait( false );
      }
      catch ( Exception ex )
      {
         HandyLogger.Log( $"Exception during request: {ex.Message}" );
         return null;
      }

      if ( response.IsSuccessStatusCode )
      {
         HandyLogger.LogRequestSuccess();
      }
      else
      {
         HandyLogger.LogRequestFail( response.StatusCode );
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
