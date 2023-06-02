using LibVLCSharp.Shared;
using System;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcFilter : IDisposable
{
   private readonly MediaPlayer _player;
   private readonly VlcMarquee _marquee;
   private readonly Equalizer _equalizer;

   public bool IsBaseBoostEnabled { get; private set; }

   public VlcFilter( MediaPlayer player, VlcMarquee marquee )
   {
      _player = player;
      _marquee = marquee;
      _equalizer = new Equalizer( 0 );
   }

   public void Dispose() => _equalizer.Dispose();

   public void SetBaseBoostEnabled( bool enable )
   {
      if ( IsBaseBoostEnabled == enable )
      {
         return;
      }

      IsBaseBoostEnabled = enable;
      if ( enable )
      {
         _equalizer.SetAmp( 15f, 0 );
         _equalizer.SetAmp( 10f, 1 );
         _equalizer.SetAmp( 5f, 2 );
      }
      else
      {
         _equalizer.SetAmp( 0f, 0 );
         _equalizer.SetAmp( 0f, 1 );
         _equalizer.SetAmp( 0f, 2 );
      }
      _player.SetEqualizer( _equalizer );
      _marquee.DisplayMarqueeText( IsBaseBoostEnabled ? "Base Boost Enabled" : "Base Boost Disabled" );
   }
}
