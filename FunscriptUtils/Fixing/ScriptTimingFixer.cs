using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing
{
   internal sealed class ScriptTimingFixer
   {
      private readonly Funscript _script;
      private readonly RoundingErrorCorrector _roundingError = new();

      public ScriptTimingFixer( Funscript script ) => _script = script;

      public void AdjustActionsToMatchBeat()
      {
         Debug.Assert( ( _script.State & FunscriptState.RoundsAndBeatsAnalyzed ) != 0 );

         int timesFixed = 0;
         foreach ( var round in _script.Rounds )
         {
            _roundingError.Reset();
            for ( int i = round.StartIndex; i < round.EndIndex; i++ )
            {
               var current = _script.Actions[i];
               if ( current.LastActionBeforeBreak )
               {
                  _roundingError.Reset();
                  continue;
               }

               var next = _script.Actions[i + 1];
               if ( FixGap( round.FullBeatTime, current, next ) )
               {
                  timesFixed++;
               }
            }
         }

         ConsoleWriter.WriteReport( "Action times fixed", timesFixed );
         ConsoleWriter.Commit();
      }

      private bool FixGap( int fullBeatTime, FunscriptAction first, FunscriptAction next )
      {
         var gap = (double)( next.Time - first.Time );

         var beatTimes = GetBeatTimes( fullBeatTime );
         var beatTime = beatTimes.FirstOrDefault( x => gap.RelativelyEqual( x ) );
         if ( Math.Abs( beatTime ) < double.Epsilon )
         {
            return false;
         }

         var newGap = (long)Math.Round( beatTime, MidpointRounding.AwayFromZero );
         _roundingError.IncrementError( beatTime - newGap );
         newGap += _roundingError.GetCorrection();

         var oldTime = next.Time;
         next.Time = first.Time + newGap;
         first.DesiredGap = newGap;
         return next.Time != oldTime;
      }

      #region Beat timing and measures

      private enum BeatMeasure
      {
         Quarter,
         Half,
         Full,
         Second,
         Fourth,
         Eighth,
         Twelveth,
         Sixteenth,
         TwentyFourth,
         ThirtySecondth
      }

      private static IEnumerable<BeatMeasure> BeatMeasures { get; } = Enum.GetValues( typeof( BeatMeasure ) ).Cast<BeatMeasure>();

      private static IEnumerable<double> GetBeatTimes( double fullBeatTime ) =>
         BeatMeasures.Select( beatMeasure => beatMeasure switch
         {
            BeatMeasure.Quarter => fullBeatTime * 4,
            BeatMeasure.Half => fullBeatTime * 2,
            BeatMeasure.Full => fullBeatTime,
            BeatMeasure.Second => fullBeatTime / 2,
            BeatMeasure.Fourth => fullBeatTime / 4,
            BeatMeasure.Eighth => fullBeatTime / 8,
            BeatMeasure.Twelveth => fullBeatTime / 12,
            BeatMeasure.Sixteenth => fullBeatTime / 16,
            BeatMeasure.TwentyFourth => fullBeatTime / 24,
            BeatMeasure.ThirtySecondth => fullBeatTime / 32,
            _ => throw new ArgumentOutOfRangeException( nameof( beatMeasure ), beatMeasure, null )
         } ).ToList();

      #endregion
   }
}