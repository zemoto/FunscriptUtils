using System.IO;
using System.Text.Json;
using VlcScriptPlayer.UI;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal static class ConfigSerializer
{
   private const string _configName = "config.json";
   private static readonly JsonSerializerOptions _serializerOptions = new()
   {
      IgnoreReadOnlyProperties = true,
   };

   public static MainViewModel ReadFromFile() => new SystemFile( _configName ).DeserializeContents<MainViewModel>();

   public static void SaveToFile( MainViewModel mainVm )
   {
      var configJson = JsonSerializer.Serialize( mainVm, _serializerOptions );
      File.WriteAllText( _configName, configJson );
   }
}