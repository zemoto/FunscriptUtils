namespace FunscriptUtils.Utils
{
   internal sealed class RoundingErrorCorrector
   {
      private double _accumulatedError;

      public int GetCorrection()
      {
         if ( _accumulatedError >= 1.0 )
         {
            _accumulatedError--;
            return 1;
         }
         if ( _accumulatedError <= -1.0 )
         {
            _accumulatedError++;
            return -1;
         }

         return 0;
      }

      public void IncrementError( double error ) => _accumulatedError += error;

      public void Reset() => _accumulatedError = 0.0;
   }
}
