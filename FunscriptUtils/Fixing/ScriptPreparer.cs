using System;
using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing
{
   internal sealed class ScriptPreparer
   {
      private const long MinMSForRoundBreak = 5000;
      private const long MinActionsPerRound = 10;

      private readonly Funscript _script;

      private bool _relativePositionsCalculated;

      public ScriptPreparer( Funscript script ) => _script = script;

      public void Prepare()
      {
         _script.Range = 100;

         CalculateRelativePositions();
         RemoveMiddleAndHoldActions();
         MaxOutActionPositions( false );
         CalculateRounds();
         CalculateBeats();
         CalculateDesiredGaps();

         for ( int i = 0; i < _script.Rounds.Count; i++ )
         {
            var round = _script.Rounds[i];
            ConsoleWriter.WriteReport( $"Round {i + 1}" );
            ConsoleWriter.WriteReport( $"Start: {round.StartTime.ToDisplayTime()}" );
            ConsoleWriter.WriteReport( $"Duration: {round.Duration.ToDisplayTime()}" );
            ConsoleWriter.WriteReport( $"Beat: {round.Beat}bpm (Full Beat: {round.FullBeatTime}ms)" );
            ConsoleWriter.Commit();
         }

         ConsoleWriter.Commit();
      }

      private void CalculateRelativePositions()
      {
         var actions = _script.Actions;
         for ( int i = 0; i < actions.Count; i++ )
         {
            FunscriptAction previous = i != 0 ? actions[i - 1] : null;
            FunscriptAction current = actions[i];
            FunscriptAction next = i != actions.Count - 1 ? actions[i + 1] : null;
            current.RelativePosition = DetermineRelativePosition( previous, current, next );
         }

         _relativePositionsCalculated = true;
      }

      private static ActionRelativePosition DetermineRelativePosition( FunscriptAction previous, FunscriptAction current, FunscriptAction next )
      {
         if ( next is null )
         {
            return previous.Position == current.Position
                ? ActionRelativePosition.Hold
                : previous.Position < current.Position ? ActionRelativePosition.Top : ActionRelativePosition.Bottom;
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

         return previous.Position < current.Position && current.Position > next.Position
             ? ActionRelativePosition.Top
             : ActionRelativePosition.Bottom;
      }

      public void RemoveMiddleAndHoldActions()
      {
         if ( !_relativePositionsCalculated )
         {
            CalculateRelativePositions();
         }

         var oldCount = _script.Actions.Count;
         _script.Actions.RemoveAll( x => x.RelativePosition is ActionRelativePosition.Middle or ActionRelativePosition.Hold );

         var actionsRemoved = oldCount - _script.Actions.Count;
         ConsoleWriter.WriteReport( "Removing middle and hold actions", actionsRemoved );
      }

      public void MaxOutActionPositions( bool forceMax )
      {
         if ( !_relativePositionsCalculated )
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

      private void CalculateDesiredGaps()
      {
         foreach ( var round in _script.Rounds )
         {
            var gapTracker = new GapTracker();
            for ( int i = round.StartIndex; i <= round.EndIndex; i++ )
            {
               var current = _script.Actions[i];
               if ( i == round.EndIndex || gapTracker.IsGapAfterActionABreak( _script.Actions, i ) )
               {
                  current.LastActionBeforeBreak = true;
                  continue;
               }

               var next = _script.Actions[i + 1];
               var gap = next.Time - current.Time;
               current.DesiredGap = gap;
               gapTracker.TrackGap( gap );
            }
         }

         // Special case if current is a singular break in the pattern.
         // Its' desired pattern should be that of the surrounding pattern.
         // Need to detect this after all desired gaps are set.
         for ( int i = 2; i < _script.Actions.Count - 1; i++ )
         {
            var prevPrev = _script.Actions[i - 2];
            var prev = _script.Actions[i - 1];
            var current = _script.Actions[i];
            var next = _script.Actions[i + 1];

            if ( prevPrev.LastActionBeforeBreak || next.LastActionBeforeBreak )
            {
               continue;
            }

            if ( prev.LastActionBeforeBreak && current.DesiredGap > next.DesiredGap ) // Current is first actions after break
            {
               current.DesiredGapMaster = next;
            }
            else if ( current.LastActionBeforeBreak && prev.DesiredGap > prevPrev.DesiredGap )
            {
               prev.DesiredGapMaster = prevPrev;
            }
            else if ( prevPrev.DesiredGap.RelativelyEqual( next.DesiredGap ) && !current.DesiredGap.RelativelyEqual( prevPrev.DesiredGap ) && current.DesiredGap > prevPrev.DesiredGap )
            {
               current.DesiredGapMaster = prevPrev;
               if ( !prev.DesiredGap.RelativelyEqual( prevPrev.DesiredGap ) && prev.DesiredGap > prevPrev.DesiredGap )
               {
                  prev.DesiredGapMaster = prevPrev;
               }
            }
         }
      }

      private void CalculateRounds()
      {
         var roundGapTracker = new GapTracker();
         var roundStartIdx = 0;
         for ( var i = 0; i < _script.Actions.Count - 1; i++ )
         {
            var current = _script.Actions[i];
            var next = _script.Actions[i + 1];
            var gap = next.Time - current.Time;

            if ( roundGapTracker.GetNumTrackedGaps() >= MinActionsPerRound && roundGapTracker.IsGapAfterActionABreak( _script.Actions, i ) && gap >= MinMSForRoundBreak )
            {
               _script.Rounds.Add( new FapHeroRound( _script, roundStartIdx, i ) );
               roundStartIdx = i + 1;
               roundGapTracker.Reset();
            }
            else
            {
               roundGapTracker.TrackGap( gap );
            }
         }

         _script.Rounds.Add( new FapHeroRound( _script, roundStartIdx, _script.Actions.Count - 1 ) );
      }

      private void CalculateBeats()
      {
         const double beatsInMinute = 60000.0;
         const long fullBeat = 469; // Based on 128 BPM
         const long halfBeat = fullBeat / 2;
         const long quarterBeat = fullBeat / 4;

         static bool GapsAreClose( long left, long right ) => Math.Abs( left - right ) < 75;

         foreach ( var round in _script.Rounds )
         {
            while ( round.Beat == default )
            {
               var randomIndex = new Random().Next( round.StartIndex, round.EndIndex - 1 );
               var gap = _script.Actions[randomIndex + 1].Time - _script.Actions[randomIndex].Time;

               if ( GapsAreClose( gap, fullBeat ) )
               {
                  round.Beat = (int)Math.Round( beatsInMinute / gap, MidpointRounding.AwayFromZero );
               }
               else if ( GapsAreClose( gap, halfBeat ) )
               {
                  round.Beat = (int)Math.Round( beatsInMinute / gap / 2.0, MidpointRounding.AwayFromZero );
               }
               else if ( GapsAreClose( gap, quarterBeat ) )
               {
                  round.Beat = (int)Math.Round( beatsInMinute / gap / 4.0, MidpointRounding.AwayFromZero );
               }
            }
         }
      }
   }
}