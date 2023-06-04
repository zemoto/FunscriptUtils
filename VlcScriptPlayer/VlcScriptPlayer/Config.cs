using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal abstract class Config : ViewModelBase
{
   private const string _configName = "config.json";
   private static readonly string _configFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), _configName );

   public static T ReadFromFile<T>() where T : Config, new()
   {
      if ( !File.Exists( _configFilePath ) )
      {
         return new T();
      }

      var configString = File.ReadAllText( _configFilePath );
      var config = JsonSerializer.Deserialize<T>( configString );

      if ( !File.Exists( config._videoFilePath ) )
      {
         config._videoFilePath = string.Empty;
      }

      if ( !File.Exists( config._scriptFilePath ) )
      {
         config._scriptFilePath = string.Empty;
      }

      return config;
   }

   public void SaveToFile()
   {
      var configJson = JsonSerializer.Serialize( this );
      File.WriteAllText( _configFilePath, configJson );
   }

   private string _connectionId;
   public string ConnectionId
   {
      get => _connectionId;
      set => SetProperty( ref _connectionId, value );
   }

   private int _desiredOffset;
   public int DesiredOffset
   {
      get => _desiredOffset;
      set => SetProperty( ref _desiredOffset, value );
   }

   private bool _boostBass;
   public bool BoostBass
   {
      get => _boostBass;
      set => SetProperty( ref _boostBass, value );
   }

   private bool _boostSaturation;
   public bool BoostSaturation
   {
      get => _boostSaturation;
      set => SetProperty( ref _boostSaturation, value );
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

   public ObservableCollection<string> ScriptFolders { get; set; } = new();
}