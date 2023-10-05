using LibVLCSharp.Shared;
using System;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcVolumeWrapper
{
   private const int _volumeIncrement = 5;

   private readonly MediaPlayer _player;
   private readonly VlcFilter _filter;

   private int _volume = -1;
   public int Volume
   {
      get => _volume;
      set
      {
         var newVolume = Math.Clamp( value, 0, 100 );
         if ( _volume != newVolume )
         {
            _player.Volume = _volume = newVolume;
            VolumeChanged?.Invoke( this, EventArgs.Empty );
         }
      }
   }

   public event EventHandler VolumeChanged;

   public VlcVolumeWrapper( MediaPlayer player, VlcFilter filter )
   {
      _player = player;
      _filter = filter;
   }

   public void IncrementVolume()
   {
      if ( _volume == 100 )
      {
         _filter.VolumeAmpEnabled = true;
         VolumeChanged?.Invoke( this, EventArgs.Empty );
      }
      else
      {
         Volume = _volume + _volumeIncrement;
      }
   }

   public void DecrementVolume()
   {
      if ( _filter.VolumeAmpEnabled )
      {
         _filter.VolumeAmpEnabled = false;
         VolumeChanged?.Invoke( this, EventArgs.Empty );
      }
      else
      {
         Volume = _volume - _volumeIncrement;
      }
   }
}