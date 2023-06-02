using LibVLCSharp.Shared;
using System;

namespace VlcScriptPlayer
{
   internal sealed class VlcFilter : IDisposable
   {
      private readonly MediaPlayer _player;
      private readonly Equalizer _equalizer;

      private bool _baseBoostOn;

      public VlcFilter( MediaPlayer player )
      {
         _player = player;
         _equalizer = new Equalizer( 0 );
      }

      public void Dispose()
      {
         _equalizer.Dispose();
      }

      public void ToggleBaseBoost() => SetBaseBoostEnabled( !_baseBoostOn );

      public void SetBaseBoostEnabled( bool enable )
      {
         if ( enable )
         {
            _equalizer.SetAmp( 20f, 0 );
            _equalizer.SetAmp( 13.333f, 1 );
            _equalizer.SetAmp( 6.667f, 2 );
         }
         else
         {
            _equalizer.SetAmp( 0f, 0 );
            _equalizer.SetAmp( 0f, 1 );
            _equalizer.SetAmp( 0f, 2 );
         }
         _baseBoostOn = enable;
         _player.SetEqualizer( _equalizer );
      }
   }
}
