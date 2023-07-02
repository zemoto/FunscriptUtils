using System.IO;
using System.Text;
using System.Text.Json;

namespace VlcScriptPlayer.Handy;

internal static class CSVFactory
{
   public static string FromFile( string path )
   {
      try
      {
         var extension = Path.GetExtension( path );
         if ( extension == ".funscript" )
         {
            return FunscriptToCSV( path );
         }
         else if ( extension == ".csv" )
         {
            return File.ReadAllText( path );
         }
      }
      catch { }

      return string.Empty;
   }

   private static string FunscriptToCSV( string path )
   {
      var funscriptJson = JsonDocument.Parse( File.ReadAllText( path ) );
      var actionsJson = funscriptJson.RootElement.GetProperty( "actions" );

      var sb = new StringBuilder();
      foreach ( var actionJson in actionsJson.EnumerateArray() )
      {
         var time = actionJson.GetProperty( "at" ).GetInt64();
         var pos = actionJson.GetProperty( "pos" ).GetInt32();
         sb.Append( time ).Append( ',' ).Append( pos ).Append( '\n' );
      }

      return sb.ToString();
   }
}
