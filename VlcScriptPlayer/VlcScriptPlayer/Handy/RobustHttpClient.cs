using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VlcScriptPlayer.Handy;

internal sealed class RobustHttpClient : HttpClient
{
   private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

   public RobustHttpClient()
   {
      Timeout = TimeSpan.FromSeconds( 3 );

      var retryStrategy = new RetryStrategyOptions<HttpResponseMessage>
      {
         Delay = TimeSpan.FromSeconds( 1 ),
         ShouldHandle = args => ValueTask.FromResult( args.Outcome.Exception is not null and not TaskCanceledException ),
         OnRetry = static args =>
         {
            Logger.LogError( $"Exception on attempt #{args.AttemptNumber}: {args.Outcome.Exception.Message}" );
            return default;
         }
      };
      _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().AddRetry( retryStrategy ).Build();
   }

   public async Task<HttpResponseMessage> RequestAsync( HttpMethod method, Uri endPoint, HttpContent content = null )
   {
      try
      {
         return await _pipeline.ExecuteAsync( async _ =>
         {
            var request = new HttpRequestMessage( method, endPoint ) { Content = content };
            return await SendAsync( request, CancellationToken.None );
         } );
      }
      catch
      {
         return null;
      }
   }

   public async Task<HttpResponseMessage> RequestAsync( HttpMethod method, Uri endPoint, HttpContent content, string requestName, string successMessage = "" )
   {
      try
      {
         Logger.Log( $"Sending Request: {requestName}" );

         var response = await _pipeline.ExecuteAsync( async _ =>
         {
            var request = new HttpRequestMessage( method, endPoint ) { Content = content };
            return await SendAsync( request, CancellationToken.None );
         } );

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
      catch ( TaskCanceledException )
      {
         Logger.LogError( "Request timed out" );
         return null;
      }
      catch ( Exception ex )
      {
         Logger.LogError( $"Request failed - {ex.Message}" );
         return null;
      }
   }
}
