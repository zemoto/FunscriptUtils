using System.IO;
using System.Reflection;
using System.Text.Json;
using VlcScriptPlayer.UI;

namespace VlcScriptPlayer;

internal static class ConfigSerializer
{
   private const string _configName = "config.json";
   private static readonly string _configFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), _configName );

   public static MainViewModel ReadFromFile()
   {
      if ( !File.Exists( _configFilePath ) )
      {
         return new();
      }

      var configString = File.ReadAllText( _configFilePath );
      return JsonSerializer.Deserialize<MainViewModel>( configString );
   }

   public static void SaveToFile( MainViewModel mainVm )
   {
      var configJson = JsonSerializer.Serialize( mainVm );
      File.WriteAllText( _configFilePath, configJson );
   }
}