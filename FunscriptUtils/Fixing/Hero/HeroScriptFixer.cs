using FunscriptUtils.Utils;

namespace FunscriptUtils.Fixing.Hero
{
   internal sealed partial class HeroScriptFixer
   {
      private const int HeroScriptMax = 75;

      private readonly string _filePath;
      private readonly string _sectionDescriptorFilePath;
      private readonly Funscript _script;

      public HeroScriptFixer( string filePath, string sectionDescriptorFilePath )
      {
         _filePath = filePath;
         _sectionDescriptorFilePath = sectionDescriptorFilePath;
         _script = FunscriptFactory.Load( _filePath );
      }

      public void CreateFixedScripts()
      {
         ConsoleWriter.WriteReport( "Number of actions", _script.Actions.Count );
         ConsoleWriter.Commit();

         AnalyzeAndPrepareScript();
         AdjustActionsToMatchBeat();

         CreateEasyModeScript();
         CreateHardModeScript();
      }
   }
}
