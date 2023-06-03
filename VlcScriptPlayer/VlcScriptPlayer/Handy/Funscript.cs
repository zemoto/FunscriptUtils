using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VlcScriptPlayer.Handy;

public sealed class FunscriptAction
{
	[JsonPropertyName( "pos" )]
	public int Position { get; set; }
	[JsonPropertyName( "at" )]
	public long Time { get; set; }
}

internal sealed class Funscript
{
	[JsonPropertyName( "actions" )]
	public List<FunscriptAction> Actions { get; set; }

	public string GetCSVString()
	{
		var sb = new StringBuilder();
		foreach ( var action in Actions )
		{
			sb.Append( action.Time ).Append( ',' ).Append( action.Position ).Append( '\n' );
		}

		return sb.ToString();
	}
}
