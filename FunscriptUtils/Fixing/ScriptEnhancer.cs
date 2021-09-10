using System;
using System.Collections.Generic;
using System.Linq;
using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing
{
   internal sealed class ScriptEnhancer
   {
      private const int HighSpeedLimit = 600;
      private const int SpeedLimit = 400;

      private readonly Funscript _originalScript;

      public ScriptEnhancer( Funscript script ) => _originalScript = script;

      public Funscript GetEnhancedScript( bool limitSpeed )
      {
         ConsoleWriter.WriteReport( "**Easy Mode Script**" );

         var script = new Funscript( _originalScript );

         if ( limitSpeed )
         {
            LimitActionSpeed( script );
         }

         AddHoldPositionActionsForLongerPauses( script, false );
         return script;
      }

      public Funscript GetHardModeScript( bool limitSpeed )
      {
         ConsoleWriter.WriteReport( "**Hard Mode Script**" );

         var script = new Funscript( _originalScript );
         AddHardModeActions( script );

         if ( limitSpeed )
         {
            LimitActionSpeed( script );
         }

         AddHoldPositionActionsForLongerPauses( script, true );
         return script;
      }

      private static void AddHoldPositionActionsForLongerPauses( Funscript script, bool hardMode )
      {
         var newActions = new List<FunscriptAction>();
         var actionsAdded = 0;

         for ( int i = 0; i < script.Actions.Count; i++ )
         {
            var current = script.Actions[i];
            newActions.Add( current );

            if ( i >= script.Actions.Count - 1 )
            {
               continue;
            }

            var next = script.Actions[i + 1];
            var gap = next.Time - current.Time;

            int minGapSize = (int)( Math.Abs( current.Position - next.Position ) / (double)HighSpeedLimit * 1000.0 );
            const int minHoldGapSize = 75;

            var desiredGap = next.LastActionBeforeBreak ? current.DesiredGap : next.DesiredGap;
            if ( !( hardMode && current.RelativePosition == ActionRelativePosition.Top ) &&
                 desiredGap > minGapSize &&
                 next.RelativePosition != ActionRelativePosition.Hold &&
                 !desiredGap.RelativelyEqual( gap ) &&
                 desiredGap < gap )
            {
               var newTime = next.Time - desiredGap;
               if ( newTime - current.Time > minHoldGapSize )
               {
                  var holdPositionAction = new FunscriptAction
                  {
                     Time = newTime,
                     Position = current.Position,
                     RelativePosition = ActionRelativePosition.Hold
                  };

                  newActions.Add( holdPositionAction );
                  actionsAdded++;
               }
            }
         }

         script.Actions = newActions;

         ConsoleWriter.WriteReport( "Adding hold actions", actionsAdded );
      }

      private static void AddHardModeActions( Funscript script )
      {
         int actionsAdded = 0;

         var actions = script.Actions;
         var newActions = new List<FunscriptAction>();

         for ( int i = 0; i < actions.Count; i++ )
         {
            var current = actions[i];
            newActions.Add( new FunscriptAction( current ) { Position = 0, RelativePosition = ActionRelativePosition.Bottom } );

            if ( i != actions.Count - 1 )
            {
               var next = actions[i + 1];

               long newTime;
               int newPosition;
               var desiredGap = next.LastActionBeforeBreak ? current.DesiredGap : next.DesiredGap;
               if ( current.LastActionBeforeBreak )
               {
                  var holdAction = new FunscriptAction
                  {
                     Time = next.Time - next.DesiredGap,
                     Position = 0,
                     DesiredGap = next.DesiredGap,
                     RelativePosition = ActionRelativePosition.Hold
                  };

                  newActions.Add( holdAction );
                  actionsAdded++;

                  newTime = next.Time - ( next.DesiredGap / 2 );
                  newPosition = 100;
               }
               else if ( desiredGap <= next.Time - current.Time )
               {
                  newTime = next.Time - ( desiredGap / 2 );
                  newPosition = Math.Min( 100, Math.Abs( current.Position - next.Position ) );
                  current.DesiredGap = newTime - current.Time;
               }
               else
               {
                  newTime = ( current.Time + next.Time ) / 2;
                  newPosition = Math.Min( 100, Math.Abs( current.Position - next.Position ) );
                  current.DesiredGap = newTime - current.Time;
               }

               var insert = new FunscriptAction
               {
                  Time = newTime,
                  Position = newPosition,
                  DesiredGap = next.Time - newTime,
                  RelativePosition = ActionRelativePosition.Top
               };

               newActions.Add( insert );
               actionsAdded++;
            }
         }

         script.Actions = newActions;

         ConsoleWriter.WriteReport( "Adding actions", actionsAdded );
      }

      private static void LimitActionSpeed( Funscript script )
      {
         bool biasHigh = false;

         // Speed limit all Top -> Bottom gaps
         foreach ( var action in script.Actions.Where( x => x.RelativePosition == ActionRelativePosition.Top && !x.LastActionBeforeBreak ) )
         {
            var variedSpeedLimit = SpeedLimit + GetRandomVariance( biasHigh );
            action.Position = variedSpeedLimit * (int)action.DesiredGap / 1000;
            if ( action.Position != 100 )
            {
               biasHigh = !biasHigh;
            }
         }

         // Speed limit all Bottom -> Top gaps, only lowering the Top action
         for ( int i = 0; i < script.Actions.Count - 1; i++ )
         {
            var current = script.Actions[i];
            if ( current.RelativePosition is not ActionRelativePosition.Bottom )
            {
               continue;
            }

            var next = script.Actions[i + 1];

            var variedSpeedLimit = SpeedLimit + GetRandomVariance( false );
            var gap = ( next.Time - current.Time) / 1000.0;
            var speed = Math.Abs( next.Position - current.Position ) / gap;

            if ( speed > variedSpeedLimit )
            {
               next.Position = (int)( SpeedLimit * gap );
            }
         }

         ConsoleWriter.WriteReport( "Speed limiting actions" );
      }

      private static readonly Random Random = new();
      private static int GetRandomVariance( bool biasHigh )
      {
         const int max = 15;
         const int min = -max;

         var bias = biasHigh ? max : min;
         var influence = biasHigh ? 1.0 : 0;

         var rnd = ( Random.NextDouble() * ( max - min ) ) + min;
         var mix = Random.NextDouble() * influence;
         return (int)( ( rnd * ( 1 - mix ) ) + ( bias * mix ) );
      }
   }
}