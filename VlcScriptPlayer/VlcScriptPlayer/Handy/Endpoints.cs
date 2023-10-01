namespace VlcScriptPlayer.Handy;

// https://www.handyfeeling.com/api/handy/v2/docs/
internal static class Endpoints
{
	public const string RootEndpoint = "https://www.handyfeeling.com/api/handy/v2/";
	public const string CheckConnectionEndpoint = $"{RootEndpoint}connected";
	public const string ServerClockEndpoint = $"{RootEndpoint}servertime";
	public const string ModeEndpoint = $"{RootEndpoint}mode";
	public const string OffsetEndpoint = $"{RootEndpoint}hstp/offset";
	public const string SetupEndpoint = $"{RootEndpoint}hssp/setup";
	public const string PlayEndpoint = $"{RootEndpoint}hssp/play";
	public const string StopEndpoint = $"{RootEndpoint}hssp/stop";
	public const string SlideEndpoint = $"{RootEndpoint}slide";

	public const string UploadCSVEndpoint = "https://www.handyfeeling.com/api/sync/upload?local=true";
}
