using System;

namespace FunscriptUtils.Generating
{
   internal sealed class ActionGenerator
   {
      private int _nextPosition;
      public FunscriptAction GetNextAction( long time )
      {
         var action = new FunscriptAction
         {
            Position = _nextPosition,
            Time = time
         };

         _nextPosition = _nextPosition switch
         {
            0 => 100,
            100 => 0,
            _ => throw new ArgumentException( "How the heck did this happen?" )
         };

         return action;
      }
   }
}
