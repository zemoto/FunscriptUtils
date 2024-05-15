using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace VlcScriptPlayer;

internal sealed class Logger : ObservableObject
{
   public static readonly Logger Instance = new();

   public string LogData { get; private set; } = "Logging Initialized";

   public static void Log( string message ) => Instance.LogEvent( message );

   public static void LogError( string message ) => Instance.LogEvent( $"ERROR: {message}" );

   private void LogEvent( string message )
   {
      LogData += Environment.NewLine;
      LogData += message;
      OnPropertyChanged( nameof( LogData ) );
   }
}
