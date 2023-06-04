using LibVLCSharp.Shared;
using System;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal interface IFilterConfig
{
   bool BoostBass { get; }
   bool BoostSaturation { get; }
}

internal sealed class VlcFilter : ViewModelBase, IDisposable
{
   [Flags]
   private enum EqualizerUpdateType
   {
      None = 0,
      Volume = 1,
      Bass = 2,
      All = Volume | Bass
   }

   private readonly MediaPlayer _player;
   private readonly VlcMarquee _marquee;
   private readonly Equalizer _equalizer;

   public VlcFilter( MediaPlayer player, VlcMarquee marquee )
   {
      _player = player;
      _marquee = marquee;
      _equalizer = new Equalizer( 0 );
   }

   public void Dispose() => _equalizer.Dispose();

   public void SetFilters( IFilterConfig filterConfig )
   {
      _volumeAmpEnabled = false; // Always disable volume amp initially
      _bassBoostEnabled = filterConfig.BoostBass;
      SetEqualizer( EqualizerUpdateType.All );

      _saturationBoostEnabled = filterConfig.BoostSaturation;
      _player.SetAdjustInt( VideoAdjustOption.Enable, 1 );
      _player.SetAdjustFloat( VideoAdjustOption.Saturation, _saturationBoostEnabled ? 1.5f : 1f );
   }

   // This must be called before MediaPlayer.Stop() or else an Access Violation will occur when libvlc unloads the filters module.
   // This also has to be done while the video window is still loaded or else the change won't be registered by the player.
   public void UnsetFilters()
   {
      _player.SetAdjustInt( VideoAdjustOption.Enable, 0 );
   }

   private void SetEqualizer( EqualizerUpdateType updateType )
   {
      if ( updateType.HasFlag( EqualizerUpdateType.Volume ) )
      {
         _equalizer.SetPreamp( _volumeAmpEnabled ? 20 : 12 );
      }
      if ( updateType.HasFlag( EqualizerUpdateType.Bass ) )
      {
         _equalizer.SetAmp( _bassBoostEnabled ? 15f : 0f, 0 );
         _equalizer.SetAmp( _bassBoostEnabled ? 7.5f : 0f, 1 );
      }

      _player.SetEqualizer( _equalizer );
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
            _marquee.DisplayMarqueeText( value ? "Bass Boost Enabled" : "Bass Boost Disabled" );
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
