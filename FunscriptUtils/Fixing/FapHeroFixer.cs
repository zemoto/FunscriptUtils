using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing
{
   internal sealed class FapHeroFixer
   {
      private readonly string _filePath;
      private readonly bool _createHardMode;
      private readonly bool _limitSpeed;

      public FapHeroFixer( string filePath, bool createHardMode, bool limitSpeed )
      {
         _filePath = filePath;
         _createHardMode = createHardMode;
         _limitSpeed = limitSpeed;
      }

      public void FixFunscript()
      {
         var funscript = FunscriptFactory.Load( _filePath );

         ConsoleWriter.WriteReport( "Number of actions", funscript.Actions.Count );
         ConsoleWriter.Commit();

         new ScriptCleaner( funscript ).Clean(); // 1 - Prepare the script by calculating basic metadata and removing noise
         new ScriptAnalyzer( funscript ).Analyze(); // 2 - Analyze the script, determining where the rounds start/end and their beat
         new ScriptTimingFixer( funscript ).AdjustActionsToMatchBeat(); // 3 - Adjust action timing to match the calculated beat

         // 3a - Improve an easy mode script, adding hold actions to enhance the patterns
         var enhancer = new ScriptEnhancer( funscript );
         var easyScript = enhancer.GetEnhancedScript( _limitSpeed );
         easyScript.Save( _filePath, _createHardMode ? "Easy" : string.Empty );

         // 3b - Create a hard-mode script that adds an extra action for each action, and adds hold actions differently
         if ( _createHardMode )
         {
            var hardScript = enhancer.GetHardModeScript( _limitSpeed );
            hardScript.Save( _filePath, "Hard" );
         }
      }
   }
}
