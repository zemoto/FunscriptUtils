using System;
using System.Collections.Generic;
using System.IO;
using FunscriptUtils.Fixing;
using FunscriptUtils.Generating;

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
         var generateScript = args[0].Contains( 'G', StringComparison.InvariantCultureIgnoreCase );
         var cleanScript = args[0].Contains( 'C', StringComparison.InvariantCultureIgnoreCase );
         var maxScript = args[0].Contains( 'M', StringComparison.InvariantCultureIgnoreCase );

         var filePath = args[1];
         if ( !File.Exists( filePath ) )
         {
            throw new ArgumentException( "Could not find file" );
         }

         if ( fixScript )
         {
            var createHardMode = args[0].Contains( 'H', StringComparison.InvariantCultureIgnoreCase );
            var scriptFixer = new FapHeroFixer( filePath, createHardMode, false );
            scriptFixer.FixFunscript();
         }
         else if ( generateScript )
         {
            using var generator = new FapHeroGenerator( GenerationParams.FromFile( filePath ) );
            generator.GenerateFunscript();
         }
         else if ( cleanScript )
         {
            var funscript = FunscriptFactory.Load( filePath );
            new ScriptCleaner( funscript ).Clean( true );

            funscript.Save( filePath, "cleaned" );
         }
         else if ( maxScript )
         {
            var funscript = FunscriptFactory.Load( filePath );
            new ScriptCleaner( funscript ).MaxOutActionPositions( true );

            funscript.Save( filePath, "maxed" );
         }
      }
   }
}