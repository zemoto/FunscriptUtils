using System;

namespace FunscriptUtils.Utils
{
   internal sealed class ConsoleWriter
   {
      private static readonly ConsoleWriter Instance = new();

      public static void WriteReport( string message ) => Instance.Write( message, true );
      public static void WriteReport( string message, int count, bool updateLast = false ) => Instance.WriteReportEx( message, count, updateLast );
      public static void WriteReport( string message, string value, bool updateLast = false ) => Instance.WriteReportEx( message, value, updateLast );
      public static void Commit() => Instance.Write( string.Empty, true );

      private int _lastMessageLength;

      private void WriteReportEx( string message, int count, bool updateLast )
      {
         if ( count > 0 )
         {
            WriteReportEx( message, count.ToString(), updateLast );
         }
      }

      private void WriteReportEx( string message, string value, bool updateLast )
      {
         var fullMessage = $"{message}: {value}";
         if ( updateLast )
         {
            UpdateLastMessage( fullMessage );
         }
         else
         {
            Write( fullMessage, _lastMessageLength > 0 );
         }
      }

      private void Write( string message, bool newLine )
      {
         _lastMessageLength = message.Length;
         if ( newLine )
         {
            Console.Write( Environment.NewLine );
         }

         Console.Write( message );
      }

      private void UpdateLastMessage( string message )
      {
         string updatedMessage = '\r' + message;
         if ( message.Length < _lastMessageLength )
         {
            updatedMessage += new string( new string( ' ', _lastMessageLength - message.Length ) );
         }

         Write( updatedMessage, false );
         _lastMessageLength = message.Length;
      }
   }
}