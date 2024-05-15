using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using System;

namespace VlcScriptPlayer.Vlc.Filter;

internal sealed partial class VlcFilter( MediaPlayer player, MarqueeViewModel marquee ) : ObservableObject, IDisposable
{
   [Flags]
   private enum EqualizerUpdateType
   {
      None = 0,
      Volume = 1,
      Bass = 2,
      All = Volume | Bass
   }

   private Equalizer _equalizer;
   private float _defaultBassValue;
   private float _defaultPreampValue;

   public void Dispose() => _equalizer?.Dispose();

   public void SetFilters( FilterViewModel filterConfig )
   {
      _equalizer?.Dispose();
      _equalizer = new Equalizer( (uint)filterConfig.EqualizerPreset );

      _defaultBassValue = _equalizer.Amp( 0 );
      _defaultPreampValue = _equalizer.Preamp;

      _volumeAmpEnabled = false; // Always disable volume amp initially
      _bassBoostEnabled = filterConfig.BoostBass;
      SetEqualizer( EqualizerUpdateType.All );

      _saturationBoostEnabled = filterConfig.BoostSaturation;
      player.SetAdjustInt( VideoAdjustOption.Enable, 1 );
      player.SetAdjustFloat( VideoAdjustOption.Saturation, _saturationBoostEnabled ? 1.5f : 1f );
   }

   // This must be called before MediaPlayer.Stop() or else an Access Violation will occur when libvlc unloads the filters module.
   public void UnsetFilters()
   {
      player.SetAdjustInt( VideoAdjustOption.Enable, 0 );
   }

   private void SetEqualizer( EqualizerUpdateType updateType )
   {
      if ( updateType.HasFlag( EqualizerUpdateType.Volume ) )
      {
         _ = _equalizer.SetPreamp( VolumeAmpEnabled ? 20f : _defaultPreampValue );
      }
      if ( updateType.HasFlag( EqualizerUpdateType.Bass ) )
      {
         _ = _equalizer.SetAmp( BassBoostEnabled ? 20f : _defaultBassValue, 0 );
      }

      _ = player.SetEqualizer( _equalizer );
   }

   [ObservableProperty]
   private bool _volumeAmpEnabled;
   partial void OnVolumeAmpEnabledChanged( bool value ) => SetEqualizer( EqualizerUpdateType.Volume );

   [ObservableProperty]
   private bool _bassBoostEnabled;
   partial void OnBassBoostEnabledChanged( bool value )
   {
      SetEqualizer( EqualizerUpdateType.Bass );
      marquee.SetText( value ? "Bass Boost Enabled" : "Bass Boost Disabled" );
   }

   [ObservableProperty]
   private bool _saturationBoostEnabled;
   partial void OnSaturationBoostEnabledChanged( bool value )
   {
      player.SetAdjustFloat( VideoAdjustOption.Saturation, value ? 1.5f : 1f );
      marquee.SetText( value ? "Saturation Boost Enabled" : "Saturation Boost Disabled" );
   }
}
