using System;
using ZemotoCommon;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyToken
{
   private static readonly SystemFile _tokenFile = new( "token.json" );

   public static HandyToken ReadFromFile() => _tokenFile.DeserializeContents<HandyToken>();

   public HandyToken()
   {
   }

   public HandyToken( string token, string connectionId, DateTime expirationTime )
   {
      Token = token;
      ConnectionId = connectionId;
      ExpirationTime = expirationTime;
   }

   public void SaveToFile() => _tokenFile.SerializeInto( this );

   public bool IsValid( string connectionId ) => DateTime.UtcNow + TimeSpan.FromMinutes( 10 ) < ExpirationTime && connectionId.Equals( ConnectionId, StringComparison.Ordinal );

   public string Token { get; set; }
   public string ConnectionId { get; set; }
   public DateTime ExpirationTime { get; set; }
}
