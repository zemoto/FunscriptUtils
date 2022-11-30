using System;
using System.Collections.Generic;

namespace FunscriptUtils.Fixing
{
   internal sealed class ScriptVibrator
   {
      private const int VibratorSpeedLimit = 400;
      private const int TimeBetweenVibrations = 45;

      // Calculated from above
      private const int MaxVibrationDistance = 18;
      private const int HalfMaxVibrationDistance = 9;

      private readonly Funscript _script;

      public ScriptVibrator( Funscript script ) => _script = script;

      public void Vibrate()
      {
         var newActions = new List<FunscriptAction>();

         for ( int i = 0; i < _script.Actions.Count - 1; i++ )
         {
            var current = _script.Actions[i];
            newActions.Add( current );

            var next = _script.Actions[i + 1];

            if ( current.GetSpeedToAction( next ) >= VibratorSpeedLimit )
            {
               continue; // No room to vibrate
            }

            var vibrationActions = GenerateVibrationActions( current, next );
            if ( vibrationActions is not null )
            {
               newActions.AddRange( vibrationActions );
            }
         }

         newActions.Add( _script.Actions[_script.Actions.Count - 1] ); // Add last action

         _script.Actions = newActions;
      }

      private static List<FunscriptAction> GenerateVibrationActions( FunscriptAction start, FunscriptAction end )
      {
         var timePoints = new List<long>();
         var currentTime = start.Time + TimeBetweenVibrations;
         while ( currentTime < end.Time - TimeBetweenVibrations )
         {
            timePoints.Add( currentTime );
            currentTime += TimeBetweenVibrations;
         }

         // Not enough time between the start and end
         if ( timePoints.Count == 0 )
         {
            return null;
         }

         // Account for start being at the top or bottom
         int bottom, top;
         if ( start.Position >= 50 )
         {
            top = Math.Min( start.Position + HalfMaxVibrationDistance, 100 );
            bottom = top - MaxVibrationDistance;
         }
         else
         {
            bottom = Math.Max( start.Position - HalfMaxVibrationDistance, 0 );
            top = bottom + MaxVibrationDistance;
         }

         double slope = ( end.Position - start.Position ) / (double)( end.Time - start.Time );
         double intercept = start.Position - ( slope * start.Time );

         var vibrationActions = new List<FunscriptAction>();
         bool addingBottom = slope < 0 || ( slope == 0 && start.Position > 100 - HalfMaxVibrationDistance ); // Going down or straight line near the top
         foreach ( var time in timePoints )
         {
            var centerLineChangeAtTime = ( ( slope * time ) + intercept ) - start.Position;
            int position = addingBottom ? (int)( bottom + centerLineChangeAtTime ) : (int)( top + centerLineChangeAtTime );

            // If we are going up and end up above/at or are going down and end up below/at the end we are done
            if ( ( slope > 0 && position > end.Position ) ||
                 ( slope < 0 && position < end.Position ) ||
                 ( slope != 0 && position == end.Position ) )
            {
               break;
            }

            var action = new FunscriptAction
            {
               Time = time,
               Position = position,
            };

            vibrationActions.Add( action );
            addingBottom = !addingBottom;
         }

         // Remove last action if its invalid
         if ( vibrationActions.Count > 0 )
         {
            var lastAction = vibrationActions[^1];
            if ( lastAction.GetSpeedToAction( end ) > VibratorSpeedLimit || lastAction.Position == end.Position )
            {
               vibrationActions.Remove( lastAction );
            }
         }

         // Evenly space the vibrations
         if ( vibrationActions.Count > 0 )
         {
            var timeBetweenActions = ( end.Time - start.Time ) / ( vibrationActions.Count + 1 );

            currentTime = start.Time + timeBetweenActions;
            foreach ( var action in vibrationActions )
            {
               action.Time = currentTime;
               currentTime += timeBetweenActions;
            }
         }

         return vibrationActions;
      }
   }
}
