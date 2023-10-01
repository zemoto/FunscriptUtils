using System.Text.Json.Serialization;

namespace VlcScriptPlayer.Handy;

internal sealed class ConnectedResponse
{
	[JsonPropertyName( "connected" )]
	public bool IsConnected { get; set; }
}

internal sealed class ServerTimeResponse
{
	[JsonPropertyName( "serverTime" )]
	public long ServerTime { get; set; }
}

internal sealed class GetOffsetResponse
{
	[JsonPropertyName( "offset" )]
	public int Offset { get; set; }
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
	[JsonPropertyName( "result" )]
	public int Result { get; set; } = -1;

	[JsonPropertyName( "error" )]
	public SetupResponseError Error { get; set; }
}

internal sealed class SetupResponseError
{
	[JsonPropertyName( "message" )]
	public string Message { get; set; }
}