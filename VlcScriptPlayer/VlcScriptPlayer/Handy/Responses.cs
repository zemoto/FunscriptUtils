using System.Text.Json.Serialization;

namespace VlcScriptPlayer.Handy;

internal sealed class GetTokenResponse
{
   [JsonPropertyName( "token" )]
   public string Token { get; set; }
}

internal sealed class ResultWrapperResponse<T>
{
   [JsonPropertyName( "result" )]
   public T Result { get; set; }

   [JsonPropertyName( "error" )]
   public ErrorResponse Error { get; set; }
}

internal sealed class ConnectedResponse
{
   [JsonPropertyName( "connected" )]
   public bool IsConnected { get; set; }
}

internal sealed class ServerTimeResponse
{
   [JsonPropertyName( "server_time" )]
   public long ServerTime { get; set; }
}

internal sealed class GetSlideResponse
{
   [JsonPropertyName( "min" )]
   public double Min { get; set; }
   [JsonPropertyName( "max" )]
   public double Max { get; set; }
}

internal sealed class UploadResponse
{
   [JsonPropertyName( "success" )]
   public bool Success { get; set; }

   [JsonPropertyName( "info" )]
   public string Info { get; set; }

   [JsonPropertyName( "url" )]
   public string Url { get; set; }
}

internal sealed class SetupResponse
{
}

internal sealed class ErrorResponse
{
   [JsonPropertyName( "message" )]
   public string Message { get; set; }
}