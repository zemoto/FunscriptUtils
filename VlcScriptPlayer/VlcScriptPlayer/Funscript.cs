using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VlcScriptPlayer;

public sealed class FunscriptAction
{
   [JsonPropertyName( "pos" )]
   public int Position { get; init; }

   [JsonPropertyName( "at" )]
   public long Time { get; init; }
}

internal sealed class Funscript
{
   public string GetCSV()
   {
      var sb = new StringBuilder();
      foreach ( var action in Actions )
      {
         _ = sb.Append( action.Time ).Append( ',' ).Append( action.Position ).Append( '\n' );
      }

      return sb.ToString();
   }

   [JsonPropertyName( "actions" )]
   public List<FunscriptAction> Actions { get; init; }
}