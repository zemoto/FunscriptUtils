using System;
using System.Net;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyLogger : ViewModelBase
{
   public static readonly HandyLogger Instance = new();

   public string LogData { get; private set; } = "Logging Initialized";

   public static void LogRequest( string requestType ) => Instance.LogEvent( $"Sending Request: {requestType}" );

   public static void LogRequestFail( HttpStatusCode statusCode ) => Instance.LogEvent( $"   Request Failed. Code: {statusCode}" );

   public static void LogRequestSuccess() => Instance.LogEvent( "   Request Success!" );

   public static void Log( string message ) => Instance.LogEvent( message );

   private void LogEvent( string message )
   {
      LogData += Environment.NewLine;
      LogData += message;
      OnPropertyChanged( nameof( LogData ) );
   }
}
