using System;
using System.Collections.Generic;
using System.Linq;

namespace VlcScriptPlayer.Buttplug;

internal sealed class VibrationAction
{
   public long Time { get; }
   public double Intensity { get; }

   public VibrationAction( long time, double intensity )
   {
      Time = time;
      Intensity = Math.Round( Math.Clamp( intensity, 0.0, 1.0 ), 2 );
   }
}

internal static class ScriptVibrationConverter
{
   private const int _actionsPerSecond = 6;
   private const int _blockInterval = 1000 / _actionsPerSecond;
   private const int _longHoldThreshold = 2000;

   public static List<VibrationAction> GenerateVibrationActions( Funscript script, int offsetMs, double intensityScale )
   {
      var vibrationActions = new List<VibrationAction>();
      for ( int i = 0; ; i++ )
      {
         var action = script.Actions[i];
         vibrationActions.Add( new VibrationAction( action.Time + offsetMs, PositionToIntensity( action.Position ) * intensityScale ) );

         if ( i == script.Actions.Count - 1 )
         {
            break;
         }

         var nextAction = script.Actions[i + 1];
         var gap = nextAction.Time - action.Time;
         if ( gap < _blockInterval * 2 )
         {
            continue;
         }

         long currentTime;
         if ( gap > _longHoldThreshold )
         {
            var lastAction = vibrationActions.Last();
            var intensityStep = lastAction.Intensity / _actionsPerSecond;
            var currentIntensity = lastAction.Intensity - intensityStep;
            currentTime = lastAction.Time + _blockInterval;
            while ( currentTime < lastAction.Time + 1000 )
            {
               vibrationActions.Add( new VibrationAction( currentTime + offsetMs, currentIntensity ) );
               currentTime += _blockInterval;
               currentIntensity -= intensityStep;
            }
            continue;
         }

         currentTime = action.Time + _blockInterval;
         while ( currentTime < nextAction.Time - ( _blockInterval / 2 ) )
         {
            vibrationActions.Add( new VibrationAction( currentTime + offsetMs, GetIntensityAtTime( script, currentTime ) * intensityScale ) );
            currentTime += _blockInterval;
         }
      }

      return vibrationActions;
   }

   private static double GetIntensityAtTime( Funscript script, long time )
   {
      var firstAction = script.Actions.Find( x => x.Time <= time );
      var nextAction = script.Actions.Find( x => x.Time > time );
      if ( firstAction is null || nextAction is null )
      {
         return 0.0;
      }

      var slope = ( nextAction.Position - firstAction.Position ) / (double)( nextAction.Time - firstAction.Time );
      var intercept = firstAction.Position - ( slope * firstAction.Time );
      var positionAtTime = (int)( ( slope * time ) + intercept );
      return PositionToIntensity( positionAtTime );
   }

   private static double PositionToIntensity( int position ) => 1.0 - ( position / 100.0 );
}
