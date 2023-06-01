using LibVLCSharp.Shared;
using System;

namespace VlcScriptPlayer
{
   internal sealed class VlcFilter : IDisposable
   {
      private readonly MediaPlayer _player;
      private readonly Equalizer _baseBoostEqualizer;

      public VlcFilter( MediaPlayer player )
      {
         _player = player;

         _baseBoostEqualizer = new Equalizer( 0 );
         _baseBoostEqualizer.SetAmp( 20f, 0 );
         _baseBoostEqualizer.SetAmp( 13.333f, 1 );
         _baseBoostEqualizer.SetAmp( 6.667f, 2 );
      }

      public void Dispose()
      {
         _baseBoostEqualizer.Dispose();
      }

      public void SetBaseBoostEnabled( bool enable )
      {
         if ( enable )
         {
            _player.SetEqualizer( _baseBoostEqualizer );
         }
         else
         {
            _player.UnsetEqualizer();
         }
      }
   }
}
