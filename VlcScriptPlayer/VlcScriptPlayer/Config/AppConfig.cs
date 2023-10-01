using System.IO;
using System.Reflection;
using System.Text.Json;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Config;

internal sealed class AppConfig : ViewModelBase
{
   private const string _configName = "config.json";
   private static readonly string _configFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), _configName );

   public static AppConfig ReadFromFile()
   {
      if ( !File.Exists( _configFilePath ) )
      {
         return new AppConfig();
      }

      var configString = File.ReadAllText( _configFilePath );
      var config = JsonSerializer.Deserialize<AppConfig>( configString );

      if ( !File.Exists( config._videoFilePath ) )
      {
         config._videoFilePath = string.Empty;
      }

      if ( !File.Exists( config._scriptFilePath ) )
      {
         config._scriptFilePath = string.Empty;
      }

      if ( !Directory.Exists( config._scriptFolder ) )
      {
         config._scriptFolder = string.Empty;
      }

      return config;
   }

   public void SaveToFile()
   {
      var configJson = JsonSerializer.Serialize( this );
      File.WriteAllText( _configFilePath, configJson );
   }

   public FilterConfig Filters { get; set; } = new FilterConfig();

   public HandyConfig Handy { get; set; } = new HandyConfig();

   private bool _forceUploadScript;
   public bool ForceUploadScript
   {
      get => _forceUploadScript;
      set => SetProperty( ref _forceUploadScript, value );
   }

   private string _videoFilePath;
   public string VideoFilePath
   {
      get => _videoFilePath;
      set => SetProperty( ref _videoFilePath, value );
   }

   private string _scriptFilePath;
   public string ScriptFilePath
   {
      get => _scriptFilePath;
      set => SetProperty( ref _scriptFilePath, value );
   }

   private string _scriptFolder;
   public string ScriptFolder
   {
      get => _scriptFolder;
      set => SetProperty( ref _scriptFolder, value );
   }
}