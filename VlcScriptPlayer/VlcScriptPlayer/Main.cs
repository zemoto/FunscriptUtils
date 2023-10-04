using System;
using System.Threading;
using System.Threading.Tasks;
using VlcScriptPlayer.Buttplug;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Main : IDisposable
{
   private readonly MainWindow _window;
   private readonly MainViewModel _model;
   private readonly VlcManager _vlc = new();
   private readonly HandyManager _handy;
   private readonly ButtplugManager _buttplug;
   private readonly ScriptManager _script;

   public Main()
   {
      _model = ConfigSerializer.ReadFromFile();
      _model.UploadScriptAndLaunchPlayerCommand = new RelayCommand<bool>( async forceUpload => await UploadScriptAndLaunchPlayerAsync( forceUpload ) );

      _handy = new HandyManager( _model.HandyVm );
      _script = new ScriptManager( _model.ScriptVm);
      _buttplug = new ButtplugManager( _model.ButtplugVm );
      _window = new MainWindow( _model );
   }

   public void Dispose()
   {
      ConfigSerializer.SaveToFile( _model );
      _handy.Dispose();
      _vlc.Dispose();
      _buttplug.Dispose();
   }

   public void Start() => _window.Show();

   private async Task UploadScriptAndLaunchPlayerAsync( bool forceUpload )
   {
      if ( !_script.VerifyPaths() )
      {
         Logger.Log( "Error: Could not find script or video file" );
         return;
      }

      var synchronizer = new VlcScriptSynchronizer( _vlc, _handy, _buttplug );
      await using ( synchronizer.ConfigureAwait( true ) )
      {
         if ( !await synchronizer.SetupSyncAsync( _model.ScriptVm.ScriptFilePath, forceUpload ).ConfigureAwait( true ) )
         {
            return;
         }

         _window.Hide();
         var videoPlayer = new VideoPlayerWindow( _vlc );
         videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _model.ScriptVm.VideoFilePath, _model.FilterVm );
         videoPlayer.Closing += ( _, _ ) => _ = ThreadPool.QueueUserWorkItem( _ => _vlc.CloseVideo() );

         videoPlayer.ShowDialog();
      }

      await Task.Delay( 1000 ).ConfigureAwait( true ); // Give time to cleanup

      await _handy.SyncLocalRangeWithDeviceRangeAsync().ConfigureAwait( true );

      _window.Show();
      _window.Activate();
   }
}
