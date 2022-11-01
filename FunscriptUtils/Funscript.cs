using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using FunscriptUtils.Utils;

namespace FunscriptUtils
{
   internal class Funscript
   {
      public Funscript() => Sections = new List<ScriptSection>();

      public Funscript( Funscript other )
      {
         Version = other.Version;
         Inverted = other.Inverted;
         Range = other.Range;
         Actions = other.Actions.ConvertAll( x => new FunscriptAction( x ) );
         Sections = other.Sections.ConvertAll( x => new ScriptSection( x ) );
      }

      public void Save( string oldFileName, string suffix = "" )
      {
         var newFileName = Path.GetFileNameWithoutExtension( oldFileName );
         if ( !string.IsNullOrEmpty( suffix ) )
         {
            newFileName += $"-{suffix}";
         }

         var newFilePath = Path.Combine( Path.GetDirectoryName( oldFileName ), $"{newFileName}.funscript" );

         var data = JsonConvert.SerializeObject( this, Formatting.None );
         File.WriteAllText( newFilePath, data );

         ConsoleWriter.WriteReport( $"Wrote funscript to {newFilePath}" );
         ConsoleWriter.Commit();
      }

      [JsonProperty( PropertyName = "version" )]
      public string Version { get; set; }

      [JsonProperty( PropertyName = "inverted" )]
      public bool Inverted { get; set; }

      [JsonProperty( PropertyName = "range" )]
      public int Range { get; set; }

      [JsonProperty( PropertyName = "actions" )]
      public List<FunscriptAction> Actions { get; set; }

      [JsonIgnore]
      public List<ScriptSection> Sections { get; }
   }

   internal sealed class ScriptSection
   {
      private readonly Funscript _parent;

      public ScriptSection( Funscript parent, int startIdx, int endIdx )
      {
         _parent = parent;
         StartIndex = startIdx;
         EndIndex = endIdx;
      }

      public ScriptSection( ScriptSection other )
      {
         _parent = other._parent;
         Beat = other.Beat;
         StartIndex = other.StartIndex;
         EndIndex = other.EndIndex;
      }

      public int Beat { get; set; }
      public int FullBeatTime => (int)Math.Round( 60000.0 / Beat, MidpointRounding.AwayFromZero );

      public int StartIndex { get; }
      public TimeSpan StartTime => TimeSpan.FromMilliseconds( _parent.Actions[StartIndex].Time );

      public int EndIndex { get; }

      public TimeSpan Duration => TimeSpan.FromMilliseconds( _parent.Actions[EndIndex].Time - _parent.Actions[StartIndex].Time );
   }
}