using System.IO;
using System.Reflection;
using System.Text.Json;
using ZemotoCommon;
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
      _ = config.VerifyPaths();

      return config;
   }

   public void SaveToFile()
   {
      var configJson = JsonSerializer.Serialize( this );
      File.WriteAllText( _configFilePath, configJson );
   }

   public bool VerifyPaths()
   {
      bool pathsValid = true;
      if ( !File.Exists( _videoFilePath ) )
      {
         VideoFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !File.Exists( _scriptFilePath ) )
      {
         ScriptFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !string.IsNullOrEmpty( _scriptFolder ) && !Directory.Exists( _scriptFolder ) )
      {
         ScriptFolder = string.Empty;
         pathsValid = false;
      }

      return pathsValid;
   }

   public FilterConfig Filters { get; set; } = new FilterConfig();

   public HandyConfig Handy { get; set; } = new HandyConfig();

   private string _videoFilePath;
   public string VideoFilePath
   {
      get => _videoFilePath;
      set
      {
         if ( SetProperty( ref _videoFilePath, value ) )
         {
            OnPropertyChanged( nameof( DisplayedVideoFilePath ) );
         }
      }
   }

   public string DisplayedVideoFilePath => FileUtils.AbbreviatePath( _videoFilePath );

   private string _scriptFilePath;
   public string ScriptFilePath
   {
      get => _scriptFilePath;
      set
      {
         if ( SetProperty( ref _scriptFilePath, value ) )
         {
            OnPropertyChanged( nameof( DisplayedScriptFilePath ) );
         }
      }
   }

   public string DisplayedScriptFilePath => FileUtils.AbbreviatePath( _scriptFilePath );

   private string _scriptFolder;
   public string ScriptFolder
   {
      get => _scriptFolder;
      set => SetProperty( ref _scriptFolder, value );
   }
}