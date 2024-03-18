using System.IO;
using System.Text.Json;
using VlcScriptPlayer.UI;

namespace VlcScriptPlayer;

internal static class ConfigSerializer
{
   private const string _configName = "config.json";

   public static MainViewModel ReadFromFile()
   {
      if ( !File.Exists( _configName ) )
      {
         return new();
      }

      var configString = File.ReadAllText( _configName );
      return JsonSerializer.Deserialize<MainViewModel>( configString );
   }

   public static void SaveToFile( MainViewModel mainVm )
   {
      var configJson = JsonSerializer.Serialize( mainVm );
      File.WriteAllText( _configName, configJson );
   }
}