using System.Collections.Generic;
using System.Linq;

namespace FunscriptUtils.Fixing
{
   internal sealed class GapTracker
   {
      private readonly Queue<long> _values = new();

      public void Reset() => _values.Clear();

      public void TrackGap( long newValue ) => _values.Enqueue( newValue );

      public bool HasGaps() => _values.Count > 0;

      public int GetNumTrackedGaps() => _values.Count;

      public bool IsGapAfterActionABreak( List<FunscriptAction> actions, int idx )
      {
         if ( idx >= actions.Count - 2 )
         {
            return false;
         }

         var current = actions[idx];
         var next = actions[idx + 1];
         var nextNext = actions[idx + 2];

         var gap = next.Time - current.Time;
         var nextGap = nextNext.Time - next.Time;

         return gap > 5000 || ( HasGaps() && gap > 3 * GetAverageGap() && gap > 3 * nextGap );
      }

      public long GetAverageGap() => _values.ToArray().Sum() / _values.Count;
   }
}