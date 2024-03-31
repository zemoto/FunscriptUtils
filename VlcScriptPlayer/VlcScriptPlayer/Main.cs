using System;
using System.Threading.Tasks;
using System.Windows;
using VlcScriptPlayer.Buttplug;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Main : IAsyncDisposable
{
   private readonly MainWindow _window;
   private readonly MainViewModel _model;
   private readonly VlcManager _vlc;
   private readonly HandyManager _handy;
   private readonly ButtplugManager _buttplug;
   private readonly ScriptManager _script;
   private readonly HotkeyManager _hotkeyManager;

   private bool _playerOpen;

   public Main()
   {
      _model = ConfigSerializer.ReadFromFile();
      _model.UploadScriptAndLaunchPlayerCommand = new RelayCommand( async () => await UploadScriptAndLaunchPlayerAsync(), () => !_playerOpen );

      _vlc = new VlcManager( _model.FilterVm, _model.PlaybackVm );
      _handy = new HandyManager( _model.HandyVm );
      _script = new ScriptManager( _model.ScriptVm );
      _buttplug = new ButtplugManager( _model.ButtplugVm );
      _hotkeyManager = new HotkeyManager( _vlc, _handy, _script );

      _window = new MainWindow( _model );
      _window.Closed += OnMainWindowClosed;
   }

   private async void OnMainWindowClosed( object sender, EventArgs e )
   {
      await DisposeAsync().ConfigureAwait( true );
      Application.Current.Shutdown();
   }

   public async ValueTask DisposeAsync()
   {
      ConfigSerializer.SaveToFile( _model );
      _handy.Dispose();
      _vlc.Dispose();
      _hotkeyManager.Dispose();
      _script.Dispose();
      await _buttplug.DisposeAsync();
   }

   public void Start() => _window.Show();

   private async Task UploadScriptAndLaunchPlayerAsync()
   {
      if ( !_script.VerifyPaths() )
      {
         Logger.LogError( "Could not find script or video file" );
         return;
      }

      using ( new ScopeGuard( () => _playerOpen = true, () => _playerOpen = false ) )
      {
         var synchronizer = new VlcScriptSynchronizer( _vlc, _script, _handy, _buttplug );
         await using ( synchronizer.ConfigureAwait( true ) )
         {
            if ( !await synchronizer.SetupSyncAsync( _model.ScriptVm.Script ).ConfigureAwait( true ) )
            {
               return;
            }

            _window.Hide();
            var videoPlayer = new VideoPlayerWindow( _vlc, _script );
            videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _model.ScriptVm.VideoFile.FullPath );

            _ = videoPlayer.ShowDialog();
         }
      }

      _window.Show();
      _ = _window.Activate();
   }
}
