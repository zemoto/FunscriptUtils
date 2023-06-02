using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace VlcScriptPlayer;

internal sealed partial class App
{
   private const string _configName = "config.json";
   private static readonly string _configFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), _configName );

   private Config _config;
   private Main _main;

   protected override void OnStartup( StartupEventArgs e )
   {
      LibVLCSharp.Shared.Core.Initialize();

      _config = ReadConfig();

      _main = new Main( _config );
      _main.Start();
   }

   protected override void OnExit( ExitEventArgs e )
   {
      SaveConfig();
      _main.Dispose();
   }

   public static Config ReadConfig()
   {
      if ( !File.Exists( _configFilePath ) )
      {
         return new Config();
      }

      var configString = File.ReadAllText( _configFilePath );
      return JsonSerializer.Deserialize<Config>( configString );
   }

   public void SaveConfig()
   {
      var configJson = JsonSerializer.Serialize( _config );
      File.WriteAllText( _configFilePath, configJson );
   }
}
