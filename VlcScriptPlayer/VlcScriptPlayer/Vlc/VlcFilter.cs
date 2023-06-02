using LibVLCSharp.Shared;
using System;

namespace VlcScriptPlayer.Vlc;

internal interface IFilterConfig
{
   bool BoostBase { get; }
   bool BoostSaturation { get; }
}

internal sealed class VlcFilter : IDisposable
{
   private readonly MediaPlayer _player;
   private readonly VlcMarquee _marquee;
   private readonly Equalizer _equalizer;

   public bool IsBaseBoostEnabled { get; private set; }
   public bool IsVolumeAmpEnabled { get; private set; }
   public bool IsSaturationBoostEnabled { get; private set; }

   public VlcFilter( MediaPlayer player, VlcMarquee marquee )
   {
      _player = player;
      _marquee = marquee;
      _equalizer = new Equalizer( 0 );
      _player.SetEqualizer( _equalizer );
      _player.SetAdjustInt( VideoAdjustOption.Enable, 1 );
   }

   public void Dispose() => _equalizer.Dispose();

   public void SetFilters( IFilterConfig filterConfig )
   {
      SetBaseBoostEnabled( filterConfig.BoostBase );
      SetSaturationBoostEnabled( filterConfig.BoostSaturation );
   }

   public void SetVolumeAmpEnabled( bool enable )
   {
      if ( IsVolumeAmpEnabled == enable )
      {
         return;
      }

      IsVolumeAmpEnabled = enable;
      _equalizer.SetPreamp( enable ? 20 : 12 );
      _player.SetEqualizer( _equalizer );
   }

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
      _marquee.DisplayMarqueeText( enable ? "Base Boost Enabled" : "Base Boost Disabled" );
   }

   public void SetSaturationBoostEnabled( bool enable )
   {
      if ( IsSaturationBoostEnabled == enable )
      {
         return;
      }

      IsSaturationBoostEnabled = enable;
      _player.SetAdjustFloat( VideoAdjustOption.Saturation, enable ? 1.5f: 1f );
      _marquee.DisplayMarqueeText( enable ? "Saturation Boost Enabled" : "Saturation Boost Disabled" );
   }
}
