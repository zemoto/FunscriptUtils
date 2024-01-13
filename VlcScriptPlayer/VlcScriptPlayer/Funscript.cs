using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace VlcScriptPlayer;

public sealed class FunscriptAction
{
   [JsonProperty( PropertyName = "pos" )]
   public int Position { get; init; }

   [JsonProperty( PropertyName = "at" )]
   public long Time { get; init; }
}

internal sealed class Funscript
{
   public static Funscript Load( string filePath ) => JsonConvert.DeserializeObject<Funscript>( File.ReadAllText( filePath ) );

   public string GetCSV()
   {
      var sb = new StringBuilder();
      foreach ( var action in Actions )
      {
         sb.Append( action.Time ).Append( ',' ).Append( action.Position ).Append( '\n' );
      }

      return sb.ToString();
   }

   [JsonProperty( PropertyName = "actions" )]
   public List<FunscriptAction> Actions { get; init; }
}