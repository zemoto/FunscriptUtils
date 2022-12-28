using System;
using System.Collections.Generic;
using System.IO;
using FunscriptUtils.Fixing;
using FunscriptUtils.Fixing.Hero;

namespace FunscriptUtils
{
   internal static class Program
   {
      private static void Main( string[] args )
      {
         try
         {
            ReadArgs( args );
         }
         catch ( Exception ex )
         {
            Console.WriteLine( $"ERROR: {ex.Message}" );
         }
         Console.ReadKey();
      }

      private static void ReadArgs( IReadOnlyList<string> args )
      {
         if ( args == null || args.Count < 2 || !args[0].StartsWith( '-' ) )
         {
            throw new ArgumentException( "Invalid Args" );
         }

         var fixScript = args[0].Contains( 'F', StringComparison.InvariantCultureIgnoreCase );
         var combineScripts = args[0].Contains( 'C', StringComparison.InvariantCultureIgnoreCase );
         var vibrateScript = args[0].Contains( 'V', StringComparison.InvariantCultureIgnoreCase );
         var separateScript = args[0].Contains( 'S', StringComparison.InvariantCultureIgnoreCase );

         var filePath = args[1];
         if ( !File.Exists( filePath ) )
         {
            throw new ArgumentException( "Could not find file" );
         }

         if ( fixScript )
         {
            string sectionDescriptorFilePath = string.Empty;
            if ( args.Count >= 3 )
            {
               sectionDescriptorFilePath = args[2];
            }

            var scriptFixer = new HeroScriptFixer( filePath, sectionDescriptorFilePath );
            scriptFixer.CreateFixedScripts();
         }
         else if ( combineScripts )
         {
            var script = FunscriptFactory.CombineScripts( filePath );
            script.Save( filePath, "combined" );
         }
         else if ( vibrateScript )
         {
            var funscript = FunscriptFactory.Load( filePath );

            var vibrator = new ScriptVibrator( funscript );
            vibrator.Vibrate();

            funscript.Save( filePath, "vibrated" );
         }
         else if ( separateScript )
         {
            if ( args.Count < 4 || !TimeSpan.TryParse( args[2], out var startTimeSpan ) || !TimeSpan.TryParse( args[3], out var duration ) )
            {
               throw new ArgumentException( "Invalid arguments. Expected: <Filepath> <StartTime in 00:00:00> <Duration in 00:00:00>" );
            }

            var script = FunscriptFactory.SeparateScript( filePath, (long)startTimeSpan.TotalMilliseconds, (long)duration.TotalMilliseconds );
            script.Save( filePath, "separated" );
         }
      }
   }
}