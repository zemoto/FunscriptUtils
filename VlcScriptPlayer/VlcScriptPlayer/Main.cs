using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VlcScriptPlayer.Buttplug;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Main : IAsyncDisposable
{
   private readonly MainWindow _window;
   private readonly MainViewModel _model;
   private readonly VlcManager _vlc = new();
   private readonly HandyManager _handy;
   private readonly ButtplugManager _buttplug;
   private readonly ScriptManager _script;
   private readonly HotkeyManager _hotkeyManager;

   public Main()
   {
      _model = ConfigSerializer.ReadFromFile();
      _model.UploadScriptAndLaunchPlayerCommand = new RelayCommand( async () => await UploadScriptAndLaunchPlayerAsync() );

      _handy = new HandyManager( _model.HandyVm );
      _script = new ScriptManager( _model.ScriptVm);
      _buttplug = new ButtplugManager( _model.ButtplugVm );
      _hotkeyManager = new HotkeyManager( _vlc );

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
      await _buttplug.DisposeAsync();
   }

   public void Start() => _window.Show();

   private async Task UploadScriptAndLaunchPlayerAsync()
   {
      if ( !_script.VerifyPaths() )
      {
         Logger.Log( "Error: Could not find script or video file" );
         return;
      }

      var synchronizer = new VlcScriptSynchronizer( _vlc, _handy, _buttplug );
      await using ( synchronizer.ConfigureAwait( true ) )
      {
         if ( !await synchronizer.SetupSyncAsync( _model.ScriptVm.ScriptFilePath ).ConfigureAwait( true ) )
         {
            return;
         }

         _window.Hide();
         var videoPlayer = new VideoPlayerWindow( _vlc );
         videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _model.ScriptVm.VideoFilePath, _model.FilterVm );
         videoPlayer.Closing += ( _, _ ) => _ = ThreadPool.QueueUserWorkItem( _ => _vlc.CloseVideo() );

         videoPlayer.ShowDialog();
      }

      _window.Show();
      _window.Activate();
   }
}
