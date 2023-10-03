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
   private readonly MainWindowViewModel _model;
   private readonly VlcManager _vlc = new();
   private readonly HandyManager _handy;
   private readonly ButtplugManager _buttplug;
   private readonly ScriptManager _script;

   public Main()
   {
      var (handyVm, filterVm, scriptVm) = ConfigSerializer.ReadFromFile();

      _handy = new HandyManager( handyVm );
      _script = new ScriptManager( scriptVm );

      var buttplugVm = new ButtplugViewModel();
      _buttplug = new ButtplugManager( buttplugVm );

      _model = new MainWindowViewModel( handyVm, buttplugVm, filterVm, scriptVm )
      {
         UploadScriptAndLaunchPlayerCommand = new RelayCommand<bool>( async forceUpload => await UploadScriptAndLaunchPlayerAsync( forceUpload ) ),
      };

      _window = new MainWindow( _model );
   }

   public void Dispose()
   {
      ConfigSerializer.SaveToFile( _model.HandyVm, _model.FilterVm, _model.ScriptVm );
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
