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
using System.Web;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyApi
{
   private const string _rootEndpoint = "https://www.handyfeeling.com/api/handy-rest/v3/";
   private const string _checkConnectionEndpoint = $"{_rootEndpoint}connected";
   private const string _serverClockEndpoint = $"{_rootEndpoint}servertime";
   private const string _modeEndpoint = $"{_rootEndpoint}mode";
   private const string _offsetEndpoint = $"{_rootEndpoint}hstp/offset";
   private const string _setupEndpoint = $"{_rootEndpoint}hssp/setup";
   private const string _playEndpoint = $"{_rootEndpoint}hssp/play";
   private const string _stopEndpoint = $"{_rootEndpoint}hssp/stop";
   private const string _syncTimeEndpoint = $"{_rootEndpoint}hssp/synctime";
   private const string _slideEndpoint = $"{_rootEndpoint}slider/stroke";
   private const string _accessTokenEndpoint = $"{_rootEndpoint}auth/token/issue";
   private const string _uploadCSVEndpoint = "https://www.handyfeeling.com/api/sync/upload?local=true";

   private readonly HttpClient _client;
   private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

   private HandyToken _accessToken;
   private long _estimatedClientServerOffset;
   private string _lastUploadedScriptSha256;

   public HandyApi( HttpClient client )
   {
      _client = client;

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

   public async Task<bool> ConnectToAndSetupHandyAsync( string connectionId )
   {
      _client.DefaultRequestHeaders.Clear();
      _lastUploadedScriptSha256 = string.Empty;
      _estimatedClientServerOffset = 0;

      if ( _accessToken?.IsValid( connectionId ) != true )
      {
         _accessToken = await GetAccessToken( connectionId );
         if ( _accessToken is null )
         {
            return false;
         }
      }
      else
      {
         Logger.Log( "Reusing access token" );
      }

      _client.DefaultRequestHeaders.Add( "X-Connection-Key", connectionId );
      _client.DefaultRequestHeaders.Add( "Authorization", $"Bearer {_accessToken.Token}" );

      return await ConnectAsync() && await SetupServerClockSyncAsync() && await EnsureModeAsync();
   }

   public async Task SetOffsetAsync( int offset )
   {
      var content = new StringContent( JsonSerializer.Serialize( new { offset } ), Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( _offsetEndpoint, content ), "SetOffset", $"Offset set to {offset}ms" );
   }

   public async Task<(double, double)> GetRangeAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( _slideEndpoint ) );
      if ( response?.IsSuccessStatusCode != true )
      {
         return (0, 0);
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var slideResponse = JsonSerializer.Deserialize<GetSlideResponse>( responseString );
      if ( slideResponse is null )
      {
         return (0, 0);
      }

      return (slideResponse.Min, slideResponse.Max);
   }

   public async Task SetRangeAsync( double min, double max )
   {
      const int minRange = 10;
      if ( min >= max - minRange )
      {
         Logger.LogError( "Invalid slide min/max range" );
         return;
      }

      var content = new StringContent( JsonSerializer.Serialize( new { min, max } ), Encoding.UTF8, "application/json" );
      using var request = await DoRequest( () => _client.PutAsync( _slideEndpoint, content ), "SetRange", $"Range set to {min}-{max}" );
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

      using var uploadResponse = await DoRequest( () => _client.PostAsync( _uploadCSVEndpoint, formData ), "UploadingScript" );
      if ( uploadResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await uploadResponse.Content.ReadAsStringAsync();
      var parsedUploadResponse = JsonSerializer.Deserialize<UploadResponse>( responseString );
      if ( parsedUploadResponse?.Success != true )
      {
         Logger.LogError( parsedUploadResponse.Info );
         return false;
      }

      var setupContent = new StringContent( JsonSerializer.Serialize( new { url = parsedUploadResponse.Url } ), Encoding.UTF8, "application/json" );
      using var setupResponse = await DoRequest( () => _client.PutAsync( _setupEndpoint, setupContent ), "SyncSetup" );
      if ( setupResponse?.IsSuccessStatusCode != true )
      {
         return false;
      }

      responseString = await setupResponse.Content.ReadAsStringAsync();

      var parsedSetupResponse = JsonSerializer.Deserialize<ResultWrapperResponse<SetupResponse>>( responseString );
      if ( parsedSetupResponse.Result is null )
      {
         Logger.LogError( parsedSetupResponse.Error.Message );
         return false;
      }

      _lastUploadedScriptSha256 = csvSha256Hash;
      return true;
   }

   public async Task PlayScriptAsync( long startTime )
   {
      var estimatedServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;
      var content = new StringContent( JsonSerializer.Serialize( new { server_time = estimatedServerTime, start_time = startTime } ), Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( _playEndpoint, content ) );
   }

   public async Task StopScriptAsync()
   {
      using var _ = await DoRequest( () => _client.PutAsync( _stopEndpoint, null ) );
   }

   public async Task SyncTimeAsync( long currentTime )
   {
      var estimatedServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _estimatedClientServerOffset;

      var syncContent = new StringContent( JsonSerializer.Serialize( new { current_time = currentTime, server_time = estimatedServerTime, filter = 0.5f } ), Encoding.UTF8, "application/json" );
      using var _ = await DoRequest( () => _client.PutAsync( _syncTimeEndpoint, syncContent ) );
   }

   private async Task<HandyToken> GetAccessToken( string connectionId )
   {
      const int ExpirationInSeconds = 14400;
      const string appId = "TURGS1RUVkxWRUkzUnpGSFUxaFRSVFV4UTBoRFJEVkZPRGcjRm9TSnAxUWlYNWVFR2t4YW5ydlgwMGxHLUhhOVNMZlplSjJOUm5NYTliTQ";

      var urlBuilder = new UriBuilder( _accessTokenEndpoint );
      var queryString = HttpUtility.ParseQueryString( string.Empty );

      queryString["apikey"] = appId;
      queryString["ck"] = connectionId;
      queryString["ttl"] = ExpirationInSeconds.ToString( CultureInfo.InvariantCulture );

      urlBuilder.Query = queryString.ToString();

      using var response = await DoRequest( () => _client.GetAsync( urlBuilder.Uri ), "GetAccessToken" );
      if ( response?.IsSuccessStatusCode != true )
      {
         return null;
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var parsedResponse = JsonSerializer.Deserialize<ResultWrapperResponse<GetTokenResponse>>( responseString );
      if ( parsedResponse.Result is not null )
      {
         return new HandyToken( parsedResponse.Result.Token, connectionId, DateTime.UtcNow + TimeSpan.FromSeconds( ExpirationInSeconds ) );
      }
      else if ( parsedResponse.Error is not null )
      {
         Logger.LogError( parsedResponse.Error.Message );
      }

      return null;
   }

   private async Task<bool> ConnectAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( _checkConnectionEndpoint ), "Connect" );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var parsedResponse = JsonSerializer.Deserialize<ResultWrapperResponse<ConnectedResponse>>( responseString );
      string error = string.Empty;
      bool connected = false;
      if ( parsedResponse.Result is not null )
      {
         connected = parsedResponse.Result.IsConnected;
      }
      else if ( parsedResponse.Error is not null )
      {
         error = parsedResponse.Error.Message;
      }

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
      const int numRequests = 15;

      Logger.Log( "Syncing clock with server clock" );
      var calculatedOffsets = new List<double>();
      for ( int i = 0; i < numRequests; i++ )
      {
         var clientSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
         using var response = await DoRequest( () => _client.GetAsync( _serverClockEndpoint ) );
         var clientReceiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
         if ( response?.IsSuccessStatusCode != true )
         {
            return false;
         }

         var serverTimeRawResponse = await response.Content.ReadAsStringAsync();
         var serverReceiveTime = JsonSerializer.Deserialize<ServerTimeResponse>( serverTimeRawResponse ).ServerTime;

         var rtd = clientReceiveTime - clientSendTime;
         var clientReceiveServerTime = serverReceiveTime + rtd / 2;

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
      var content = new StringContent( JsonSerializer.Serialize( new { mode = 1 } ), Encoding.UTF8, "application/json" );
      using var response = await DoRequest( () => _client.PutAsync( _modeEndpoint, content ), "SetMode", "Mode set to HSSP" );
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
      catch ( Exception ex )
      {
         Logger.LogError( $"Request failed: {ex.Message}" );
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

   private sealed class HandyToken( string token, string connectionId, DateTime expirationTime )
   {
      private readonly string _connectionId = connectionId;
      private readonly DateTime _expirationTime = expirationTime;

      public string Token { get; } = token;

      public bool IsValid( string connectionId ) => DateTime.UtcNow < _expirationTime && connectionId.Equals( _connectionId, StringComparison.Ordinal );
   }
}