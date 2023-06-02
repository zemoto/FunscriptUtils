using LibVLCSharp.Shared;
using System;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal interface IFilterConfig
{
   bool BoostBase { get; }
   bool BoostSaturation { get; }
}

internal sealed class VlcFilter : ViewModelBase, IDisposable
{
   private readonly MediaPlayer _player;
   private readonly VlcMarquee _marquee;
   private readonly Equalizer _equalizer;

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
      BaseBoostEnabled = filterConfig.BoostBase;
      SaturationBoostEnabled = filterConfig.BoostSaturation;
   }

   private bool _volumeAmpEnabled;
   public bool VolumeAmpEnabled
   {
      get => _volumeAmpEnabled;
      set
      {
         if ( SetProperty( ref _volumeAmpEnabled, value ) )
         {
            _equalizer.SetPreamp( value ? 20 : 12 );
            _player.SetEqualizer( _equalizer );
         }
      }
   }

   private bool _baseBoostEnabled;
   public bool BaseBoostEnabled
   {
      get => _baseBoostEnabled;
      set
      {
         if ( SetProperty( ref _baseBoostEnabled, value ) )
         {
            _equalizer.SetAmp( value ? 15f : 0f, 0 );
            _equalizer.SetAmp( value ? 7.5f : 0f, 1 );
            _player.SetEqualizer( _equalizer );
            _marquee.DisplayMarqueeText( value ? "Base Boost Enabled" : "Base Boost Disabled" );
         }
      }
   }

   private bool _saturationBoostEnabled;
   public bool SaturationBoostEnabled
   {
      get => _saturationBoostEnabled;
      set
      {
         if ( SetProperty( ref _saturationBoostEnabled, value ) )
         {
            _player.SetAdjustFloat( VideoAdjustOption.Saturation, value ? 1.5f : 1f );
            _marquee.DisplayMarqueeText( value ? "Saturation Boost Enabled" : "Saturation Boost Disabled" );
         }
      }
   }
}
