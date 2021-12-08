using System;
using System.Collections.Generic;
using System.IO;
using FunscriptUtils.Utils;
using Newtonsoft.Json;

namespace FunscriptUtils
{
   internal sealed class FunscriptFactory
   {
      public static Funscript CreateFresh() =>
         new()
         {
            Version = "1.0",
            Inverted = false,
            Range = 100,
            Actions = new List<FunscriptAction>()
         };

      public static Funscript Load( string filePath ) =>
         Path.GetExtension( filePath ) switch
         {
            ".funscript" => JsonConvert.DeserializeObject<Funscript>( File.ReadAllText( filePath ) ),
            ".csv" => FromCsv( filePath ),
            _ => throw new ArgumentException( "Invalid funscript file" )
         };

      public static Funscript CombineScripts( string combineParamsFilePath )
      {
         var scripts = new List<(Funscript, TimeSpan)>();
         var lines = File.ReadAllLines( combineParamsFilePath );
         for ( int i = 0; i < lines.Length; i += 2 )
         {
            var script = Load( lines[i] );
            var video = new VideoWrapper( lines[i + 1] );
            scripts.Add( (script, video.GetDuration()) );
         }

         var newScript = CreateFresh();
         var currentDuration = TimeSpan.Zero;

         foreach ( var (script, duration) in scripts )
         {
            foreach ( var action in script.Actions )
            {
               action.Time += (long)currentDuration.TotalMilliseconds;
            }

            newScript.Actions.AddRange( script.Actions );
            currentDuration += duration;
         }

         return newScript;
      }

      private static Funscript FromCsv( string filePath )
      {
         var funscript = new Funscript();
         var lines = File.ReadAllLines( filePath );

         funscript.Version = "1.0";
         funscript.Inverted = false;
         funscript.Range = 100;
         funscript.Actions = new List<FunscriptAction>();

         foreach ( var line in lines )
         {
            var cleanedLine = line.Trim();
            if ( cleanedLine.StartsWith( '#' ) )
            {
               continue;
            }

            var data = cleanedLine.Split( ',' );
            if ( data.Length != 2 || !int.TryParse( data[0], out var time ) || !int.TryParse( data[1], out var position ) )
            {
               return null;
            }

            var action = new FunscriptAction
            {
               Position = position,
               Time = time
            };

            funscript.Actions.Add( action );
         }

         return funscript;
      }
   }
}
