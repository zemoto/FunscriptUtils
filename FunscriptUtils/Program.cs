using System;
using System.Collections.Generic;
using System.IO;
using FunscriptUtils.Fixing;

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
         var pruneScript = args[0].Contains( 'P', StringComparison.InvariantCultureIgnoreCase );
         var maxScript = args[0].Contains( 'M', StringComparison.InvariantCultureIgnoreCase );
         var combineScripts = args[0].Contains( 'C', StringComparison.InvariantCultureIgnoreCase );
         var vibrateScript = args[0].Contains( 'V', StringComparison.InvariantCultureIgnoreCase );

         var filePath = args[1];
         if ( !File.Exists( filePath ) )
         {
            throw new ArgumentException( "Could not find file" );
         }

         if ( fixScript )
         {
            var createHardMode = args[0].Contains( 'H', StringComparison.InvariantCultureIgnoreCase );
            var scriptFixer = new HeroScriptFixer( filePath, false );
            scriptFixer.CreateFixedScripts( createHardMode );
         }
         else if ( pruneScript )
         {
            var funscript = FunscriptFactory.Load( filePath );

            var preparer = new ScriptPreparer( funscript );
            preparer.RemoveMiddleAndHoldActions();
            preparer.MaxOutActionPositions( true );

            funscript.Save( filePath, "cleaned" );
         }
         else if ( maxScript )
         {
            var funscript = FunscriptFactory.Load( filePath );
            new ScriptPreparer( funscript ).MaxOutActionPositions( true );

            funscript.Save( filePath, "maxed" );
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
      }
   }
}