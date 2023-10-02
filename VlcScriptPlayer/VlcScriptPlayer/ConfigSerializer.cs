using System.IO;
using System.Reflection;
using System.Text.Json;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer;

internal static class ConfigSerializer
{
   private const string _configName = "config.json";
   private static readonly string _configFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), _configName );

   public static (HandyViewModel, FilterViewModel, ScriptViewModel) ReadFromFile()
   {
      if ( !File.Exists( _configFilePath ) )
      {
         return (new(), new(), new());
      }

      var configString = File.ReadAllText( _configFilePath );
      var config = JsonSerializer.Deserialize<Config>( configString );

      return (config.HandyVm, config.FilterVm, config.ScriptVm);
   }

   public static void SaveToFile( HandyViewModel handyVm, FilterViewModel filterVm, ScriptViewModel scriptVm )
   {
      var config = new Config()
      {
         HandyVm = handyVm,
         FilterVm = filterVm,
         ScriptVm = scriptVm
      };

      var configJson = JsonSerializer.Serialize( config );
      File.WriteAllText( _configFilePath, configJson );
   }

   private sealed class Config
   {
      public HandyViewModel HandyVm { get; set; } = new HandyViewModel();
      public FilterViewModel FilterVm { get; set; } = new FilterViewModel();
      public ScriptViewModel ScriptVm { get; set; } = new ScriptViewModel();
   }
}