using LibVLCSharp.Shared;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcMarquee
{
   private readonly MediaPlayer _player;
   private readonly object _enabledLock = new();

   public VlcMarquee( MediaPlayer player )
   {
      _player = player;

      _player.SetMarqueeInt( VideoMarqueeOption.Position, 5 );
      _player.SetMarqueeInt( VideoMarqueeOption.Opacity, 192 );
      _player.SetMarqueeInt( VideoMarqueeOption.Timeout, 1000 );
      _player.SetMarqueeInt( VideoMarqueeOption.X, 40 );
      _player.SetMarqueeInt( VideoMarqueeOption.Y, 60 );
      _player.SetMarqueeInt( VideoMarqueeOption.Size, 110 );
   }

   public void DisplayMarqueeText( string text )
   {
      if ( !Enabled )
      {
         return;
      }

      _player.SetMarqueeString( VideoMarqueeOption.Text, text );
      _player.SetMarqueeInt( VideoMarqueeOption.Enable, 1 );
   }

   private bool enabled;
   public bool Enabled
   {
      get
      {
         lock ( _enabledLock )
         {
            return enabled;
         }
      }
      set
      {
         lock ( _enabledLock )
         {
            enabled = value;
            if ( !enabled )
            {
               _player.SetMarqueeInt( VideoMarqueeOption.Enable, 0 );
            }
         }
      }
   }
}
