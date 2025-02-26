using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace VlcScriptPlayer.Handy.Api;

internal sealed class HandyApiV3( HttpClient client ) : HandyApiBase( client )
{
   private const string _rootEndpoint = "https://www.handyfeeling.com/api/handy-rest/v3/";
   private const string _slideEndpoint = $"{_rootEndpoint}slider/stroke";
   private const string _accessTokenEndpoint = $"{_rootEndpoint}auth/token/issue";
   private const string _deviceInfoEndpoint = $"{_rootEndpoint}info";

   private HandyToken _accessToken;

   protected override Endpoints GetEndpoints() => new( _rootEndpoint, _slideEndpoint );

   protected override async Task<bool> GetDeviceCompatibleAsync()
   {
      using var response = await DoRequest( () => _client.GetAsync( _deviceInfoEndpoint ), "GetDeviceInfo" );
      if ( response?.IsSuccessStatusCode != true )
      {
         return false;
      }

      var responseString = await response.Content.ReadAsStringAsync();
      var parsedResponse = JsonSerializer.Deserialize<ResultWrapperResponse<DeviceInfoResponse>>( responseString );
      if ( parsedResponse.Result is not null &&
         !string.IsNullOrEmpty( parsedResponse.Result.Version ) &&
         int.TryParse( parsedResponse.Result.Version.AsSpan( 0, 1 ), out int deviceMajorVersion ) &&
         deviceMajorVersion >= 4 )
      {
         return true;
      }

      Logger.LogError( "Device incompatible with API v3" );
      return false;
   }

   protected override async Task<Dictionary<string, string>> GetClientHeadersAsync( string connectionId )
   {
      if ( _accessToken?.IsValid( connectionId ) != true )
      {
         _accessToken = await GetAccessToken( connectionId );
         if ( _accessToken is null )
         {
            return null;
         }
      }
      else
      {
         Logger.Log( "Reusing access token" );
      }

      return new Dictionary<string, string>
      {
         ["Authorization"] = $"Bearer {_accessToken.Token}"
      };
   }

   protected override bool ConnectionSuccessful( string responseString, out string error )
   {
      error = string.Empty;
      var parsedResponse = JsonSerializer.Deserialize<ResultWrapperResponse<ConnectedResponse>>( responseString );
      if ( parsedResponse.Result is not null )
      {
         return parsedResponse.Result.IsConnected;
      }
      else if ( parsedResponse.Error is not null )
      {
         error = parsedResponse.Error.Message;
      }

      return false;
   }

   protected override bool SetupSuccessful( string responseString, out string error )
   {
      var parsedSetupResponse = JsonSerializer.Deserialize<ResultWrapperResponse<SetupResponse>>( responseString );
      if ( parsedSetupResponse.Result is null )
      {
         error = parsedSetupResponse.Error.Message;
         return false;
      }

      error = string.Empty;
      return true;
   }

   protected override long ParseServerTimeResponse( string responseString ) => JsonSerializer.Deserialize<ServerTimeV3Response>( responseString ).ServerTime;

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
         return new HandyToken( parsedResponse.Result.Token, connectionId, DateTime.Now + TimeSpan.FromSeconds( ExpirationInSeconds ) );
      }
      else if ( parsedResponse.Error is not null )
      {
         Logger.LogError( parsedResponse.Error.Message );
      }

      return null;
   }

   protected override StringContent GetPlayScriptContent( long serverTime, long startTime ) => new( $"{{ \"server_time\": {serverTime}, \"start_time\": {startTime} }}", Encoding.UTF8, "application/json" );

   private sealed class HandyToken( string token, string connectionId, DateTime expirationTime )
   {
      private readonly string _connectionId = connectionId;
      private readonly DateTime _expirationTime = expirationTime;

      public string Token { get; } = token;

      public bool IsValid( string connectionId ) => DateTime.Now < _expirationTime && connectionId.Equals( _connectionId, StringComparison.Ordinal );
   }
}