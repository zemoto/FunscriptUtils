using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing
{
   internal sealed class ScriptCleaner
   {
      private readonly Funscript _script;

      public ScriptCleaner( Funscript script ) => _script = script;

      public void Clean( bool forceMax = false )
      {
         _script.Range = 100;

         CalculateRelativePositions();
         RemoveMiddleAndHoldActions();
         MaxOutActionPositions( forceMax );

         ConsoleWriter.Commit();
      }

      private void CalculateRelativePositions()
      {
         var actions = _script.Actions;
         for ( int i = 0; i < actions.Count; i++ )
         {
            FunscriptAction previous = null;
            FunscriptAction current = actions[i];
            FunscriptAction next = null;
            if ( i != 0 )
            {
               previous = actions[i - 1];
            }

            if ( i != actions.Count - 1 )
            {
               next = actions[i + 1];
            }

            current.RelativePosition = DetermineRelativePosition( previous, current, next );
         }

         _script.State |= FunscriptState.RelativePositionsCalculated;
      }

      private static ActionRelativePosition DetermineRelativePosition( FunscriptAction previous, FunscriptAction current, FunscriptAction next )
      {
         if ( next is null )
         {
            if ( previous.Position == current.Position )
            {
               return ActionRelativePosition.Hold;
            }

            return previous.Position < current.Position ? ActionRelativePosition.Top : ActionRelativePosition.Bottom;
         }

         if ( previous is null )
         {
            return current.Position <= next.Position ? ActionRelativePosition.Bottom : ActionRelativePosition.Top;
         }

         if ( previous.Position == current.Position )
         {
            return ActionRelativePosition.Hold;
         }

         if ( current.Position == next.Position )
         {
            return previous.Position < current.Position ? ActionRelativePosition.Top : ActionRelativePosition.Bottom;
         }

         if ( ( previous.Position < current.Position && current.Position < next.Position ) ||
              ( previous.Position > current.Position && current.Position > next.Position ) )
         {
            return ActionRelativePosition.Middle;
         }

         if ( previous.Position < current.Position && current.Position > next.Position )
         {
            return ActionRelativePosition.Top;
         }

         return ActionRelativePosition.Bottom;
      }

      private void RemoveMiddleAndHoldActions()
      {
         var oldCount = _script.Actions.Count;
         _script.Actions.RemoveAll( x => x.RelativePosition is ActionRelativePosition.Middle or ActionRelativePosition.Hold );

         var actionsRemoved = oldCount - _script.Actions.Count;
         ConsoleWriter.WriteReport( "Removing middle actions", actionsRemoved );
      }

      public void MaxOutActionPositions( bool forceMax )
      {
         if ( ( _script.State & FunscriptState.RelativePositionsCalculated ) == 0 )
         {
            CalculateRelativePositions();
         }

         var actionsMaxed = 0;
         for ( int i = 0; i < _script.Actions.Count; i++ )
         {
            var action = _script.Actions[i];
            switch ( action.RelativePosition )
            {
               case ActionRelativePosition.Top when forceMax || action.Position > 95:
               {
                  action.Position = 100;
                  actionsMaxed++;
                  break;
               }
               case ActionRelativePosition.Bottom when forceMax || action.Position < 5:
               {
                  action.Position = 0;
                  actionsMaxed++;
                  break;
               }
               case ActionRelativePosition.Hold when i > 0:
               {
                  var prev = _script.Actions[i - 1];
                  action.Position = prev.Position;
                  break;
               }
            }
         }

         ConsoleWriter.WriteReport( "Maxing out actions", actionsMaxed );
      }
   }
}