using System;
using System.Collections.Generic;
using System.Linq;
using FunscriptUtils.Utils;
using ZemotoCommon;

namespace FunscriptUtils.Fixing.Hero
{
   internal sealed class HeroScriptEnhancer
   {
      private const int HighSpeedLimit = 600;
      private const int SpeedLimit = 470;
      private const int HeroScriptMax = 75;

      private readonly Funscript _originalScript;

      public HeroScriptEnhancer( Funscript script ) => _originalScript = script;

      public Funscript GetEnhancedScript( bool limitSpeed )
      {
         ConsoleWriter.WriteReport( "**Easy Mode Script**" );

         var script = new Funscript( _originalScript );

         if ( limitSpeed )
         {
            LimitActionSpeed( script );
         }

         AddHoldPositionActionsForLongerPauses( script, false );
         AddStarterAction( script );
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
         AddStarterAction( script );
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

                  newTime = next.Time - next.DesiredGap / 2;
                  newPosition = HeroScriptMax;
               }
               else if ( desiredGap <= next.Time - current.Time )
               {
                  newTime = next.Time - desiredGap / 2;
                  newPosition = Math.Min( HeroScriptMax, Math.Abs( current.Position - next.Position ) );
                  current.DesiredGap = newTime - current.Time;
               }
               else
               {
                  newTime = ( current.Time + next.Time ) / 2;
                  newPosition = Math.Min( HeroScriptMax, Math.Abs( current.Position - next.Position ) );
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
         // Speed limit all Top -> Bottom gaps
         foreach ( var action in script.Actions.Where( x => x.RelativePosition == ActionRelativePosition.Top && !x.LastActionBeforeBreak ) )
         {
            action.Position = SpeedLimit * (int)action.DesiredGap / 1000;
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

            if ( current.GetSpeedToAction( next ) > SpeedLimit )
            {
               var gapInSeconds = ( next.Time - current.Time ) / 1000.0;
               next.Position = (int)( SpeedLimit * gapInSeconds );
            }
         }

         script.Actions.Where( x => x.RelativePosition is ActionRelativePosition.Top ).ForEach( x => x.Position = Math.Min( x.Position, HeroScriptMax ) );

         ConsoleWriter.WriteReport( "Speed limiting actions" );
      }

      private static void AddStarterAction( Funscript script )
      {
         if ( script.Actions.Count < 2 )
         {
            return;
         }

         var firstAction = script.Actions[0];
         var secondAction = script.Actions[1];

         var starterTime = firstAction.Time - secondAction.Time + firstAction.Time;
         if ( starterTime < 0 )
         {
            return;
         }

         var starterAction = new FunscriptAction
         {
            Time = starterTime,
            Position = secondAction.Position,
            DesiredGap = firstAction.DesiredGap,
            RelativePosition = secondAction.RelativePosition
         };

         script.Actions.Insert( 0, starterAction );

         if ( starterAction.RelativePosition is not ActionRelativePosition.Bottom )
         {
            AddStarterAction( script );
         }
      }
   }
}