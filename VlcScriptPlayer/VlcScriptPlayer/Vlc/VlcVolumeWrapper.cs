using LibVLCSharp.Shared;
using System;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcVolumeWrapper( MediaPlayer player, VlcFilter filter )
{
   private const int _volumeIncrement = 5;

   public event EventHandler VolumeChanged;

   private int _volume = player.Volume;
   public int Volume
   {
      get => _volume;
      set
      {
         var newVolume = Math.Clamp( value, 0, 100 );
         if ( _volume != newVolume )
         {
            player.Volume = _volume = newVolume;
            VolumeChanged?.Invoke( this, EventArgs.Empty );
         }
      }
   }

   public void IncrementVolume()
   {
      if ( _volume == 100 )
      {
         filter.VolumeAmpEnabled = true;
         VolumeChanged?.Invoke( this, EventArgs.Empty );
      }
      else
      {
         Volume = _volume + _volumeIncrement;
      }
   }

   public void DecrementVolume()
   {
      if ( filter.VolumeAmpEnabled )
      {
         filter.VolumeAmpEnabled = false;
         VolumeChanged?.Invoke( this, EventArgs.Empty );
      }
      else
      {
         Volume = _volume - _volumeIncrement;
      }
   }
}