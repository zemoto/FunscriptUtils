using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal sealed class PlaybackViewModel : ViewModelBase
{
   private bool _loop;
   public bool Loop
   {
      get => _loop;
      set => SetProperty( ref _loop, value );
   }

   private bool _autoplay;
   public bool Autoplay
   {
      get => _autoplay;
      set => SetProperty( ref _autoplay, value );
   }
}