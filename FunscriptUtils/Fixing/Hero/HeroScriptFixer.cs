using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing.Hero
{
   internal sealed class HeroScriptFixer
   {
      private readonly string _filePath;
      private readonly bool _limitSpeed;

      public HeroScriptFixer( string filePath, bool limitSpeed )
      {
         _filePath = filePath;
         _limitSpeed = limitSpeed;
      }

      public void CreateFixedScripts( bool createHardMode, string sectionDescriptorFilePath )
      {
         var funscript = FunscriptFactory.Load( _filePath );

         ConsoleWriter.WriteReport( "Number of actions", funscript.Actions.Count );
         ConsoleWriter.Commit();

         new HeroScriptPreparer( funscript, sectionDescriptorFilePath ).Prepare();
         new HeroScriptTimingFixer( funscript ).AdjustActionsToMatchBeat();

         var enhancer = new HeroScriptEnhancer( funscript );
         var easyScript = enhancer.GetEnhancedScript( _limitSpeed );
         easyScript.Save( _filePath, createHardMode ? "Easy" : string.Empty );

         if ( createHardMode )
         {
            var hardScript = enhancer.GetHardModeScript( _limitSpeed );
            hardScript.Save( _filePath, "Hard" );
         }
      }
   }
}
