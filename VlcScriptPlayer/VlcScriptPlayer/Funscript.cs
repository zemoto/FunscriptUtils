using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VlcScriptPlayer;

internal sealed class EnforceLongJsonConverter : JsonConverter<long>
{
   public override long Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) => (long)reader.GetDouble();
   public override void Write( Utf8JsonWriter writer, long value, JsonSerializerOptions options ) => writer.WriteNumberValue( value );
}

internal sealed class FunscriptMetadata
{
   [JsonPropertyName( "chapters" )]
   public List<FunscriptChapter> Chapters { get; init; }
}

internal sealed class FunscriptChapter
{
   [JsonPropertyName( "name" )]
   public string Name { get; init; }

   [JsonPropertyName( "startTime" )]
   public TimeSpan StartTime { get; init; }
}

internal sealed class FunscriptAction
{
   [JsonPropertyName( "pos" )]
   public int Position { get; init; }

   [JsonPropertyName( "at" )]
   [JsonConverter( typeof( EnforceLongJsonConverter ) )]
   public long Time { get; init; }
}

internal sealed class Funscript : IJsonOnDeserialized
{
   public void OnDeserialized()
   {
      Metadata?.Chapters?.Sort( ( x, y ) => x.StartTime.CompareTo( y.StartTime ) );
      Actions?.Sort( ( x, y ) => x.Time.CompareTo( y.Time ) );
   }

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

   [JsonPropertyName( "metadata" )]
   public FunscriptMetadata Metadata { get; init; }
}