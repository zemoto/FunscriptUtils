using System;

namespace FunscriptUtils.Generating
{
   internal sealed class SimpleGenerator
   {
      public static Funscript GenerateScript( int bpm, TimeSpan duration )
      {
         var actionGenerator = new ActionGenerator();
         var funscript = FunscriptFactory.CreateFresh();

         var totalActions = (int)( duration.TotalMinutes * bpm );
         var currentTime = 0;
         var msBetweenActions = (int)( 1000 / ( (double)bpm / 60 ) );
         for ( int i = 0; i < totalActions; i++ )
         {
            funscript.Actions.Add( actionGenerator.GetNextAction( currentTime ) );
            currentTime += msBetweenActions;
         }

         return funscript;
      }
   }
}
