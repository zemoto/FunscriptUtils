using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VlcScriptPlayer.Handy.Api;

internal sealed class HandyApiV2( HttpClient client ) : HandyApiBase( client )
{
   private const string _rootEndpoint = "https://www.handyfeeling.com/api/handy/v2/";
   private const string _slideEndpoint = $"{_rootEndpoint}slide";

   protected override Endpoints GetEndpoints() => new( _rootEndpoint, _slideEndpoint );
   protected override Task<bool> GetDeviceCompatibleAsync() => Task.FromResult( true );
   protected override Task<Dictionary<string, string>> GetClientHeadersAsync( string connectionId ) => Task.FromResult( new Dictionary<string, string>() );

   protected override bool ConnectionSuccessful( string responseString, out string error )
   {
      error = string.Empty;
      return JsonSerializer.Deserialize<ConnectedResponse>( responseString ).IsConnected;
   }

   protected override bool SetupSuccessful( string responseString, out string error )
   {
      var parsedSetupResponse = JsonSerializer.Deserialize<ResultWrapperResponse<int>>( responseString );
      if ( parsedSetupResponse.Error is not null )
      {
         error = parsedSetupResponse.Error.Message;
         return false;
      }

      error = string.Empty;
      return parsedSetupResponse.Result == 1;
   }

   protected override long ParseServerTimeResponse( string responseString ) => JsonSerializer.Deserialize<ServerTimeV2Response>( responseString ).ServerTime;

   protected override StringContent GetPlayScriptContent( long serverTime, long startTime ) => new( $"{{ \"estimatedServerTime\": {serverTime}, \"startTime\": {startTime} }}", Encoding.UTF8, "application/json" );
}
