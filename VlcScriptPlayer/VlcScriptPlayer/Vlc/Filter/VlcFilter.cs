﻿using LibVLCSharp.Shared;
using System;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc.Filter;

internal sealed class VlcFilter( MediaPlayer player, VlcMarquee marquee ) : ViewModelBase, IDisposable
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
   // This also has to be done while the video window is still loaded or else the change won't be registered by the player.
   public void UnsetFilters()
   {
      player.SetAdjustInt( VideoAdjustOption.Enable, 0 );
   }

   private void SetEqualizer( EqualizerUpdateType updateType )
   {
      if ( updateType.HasFlag( EqualizerUpdateType.Volume ) )
      {
         _equalizer.SetPreamp( _volumeAmpEnabled ? 20f : _defaultPreampValue );
      }
      if ( updateType.HasFlag( EqualizerUpdateType.Bass ) )
      {
         _equalizer.SetAmp( _bassBoostEnabled ? 20f : _defaultBassValue, 0 );
      }

      player.SetEqualizer( _equalizer );
   }

   private bool _volumeAmpEnabled;
   public bool VolumeAmpEnabled
   {
      get => _volumeAmpEnabled;
      set
      {
         if ( SetProperty( ref _volumeAmpEnabled, value ) )
         {
            SetEqualizer( EqualizerUpdateType.Volume );
         }
      }
   }

   private bool _bassBoostEnabled;
   public bool BassBoostEnabled
   {
      get => _bassBoostEnabled;
      set
      {
         if ( SetProperty( ref _bassBoostEnabled, value ) )
         {
            SetEqualizer( EqualizerUpdateType.Bass );
            marquee.DisplayText( value ? "Bass Boost Enabled" : "Bass Boost Disabled" );
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
            player.SetAdjustFloat( VideoAdjustOption.Saturation, value ? 1.5f : 1f );
            marquee.DisplayText( value ? "Saturation Boost Enabled" : "Saturation Boost Disabled" );
         }
      }
   }
}
