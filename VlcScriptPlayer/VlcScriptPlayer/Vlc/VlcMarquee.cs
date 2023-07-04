using LibVLCSharp.Shared;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcMarquee
{
   private readonly MediaPlayer _player;

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

   public void DisplayText( string text ) => _player.SetMarqueeString( VideoMarqueeOption.Text, text );

   public void SetEnabled( bool enabled )
   {
      if ( !enabled )
      {
         _player.SetMarqueeString( VideoMarqueeOption.Text, string.Empty );
      }

      _player.SetMarqueeInt( VideoMarqueeOption.Enable, enabled ? 1 : 0 );
   }
}
