using System;
using System.Net;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Logger : ViewModelBase
{
   public static readonly Logger Instance = new();

   public string LogData { get; private set; } = "Logging Initialized";

   public static void LogRequest( string requestType ) => Instance.LogEvent( $"Sending Request: {requestType}" );

   public static void LogRequestFail( HttpStatusCode statusCode ) => Instance.LogEvent( $"   Request Failed. Code: {statusCode}" );

   public static void LogRequestFail() => Instance.LogEvent( "   Request Failed." );

   public static void LogRequestSuccess() => Instance.LogEvent( "   Request Success!" );

   public static void Log( string message ) => Instance.LogEvent( message );

   private void LogEvent( string message )
   {
      LogData += Environment.NewLine;
      LogData += message;
      OnPropertyChanged( nameof( LogData ) );
   }
}
