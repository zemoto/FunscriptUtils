using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal class PlaybackViewModel : ViewModelBase
{
   private bool _loop;
	public bool Loop
	{
		get => _loop;
		set => SetProperty( ref _loop, value );
	}
}