using System;
using System.Threading;
using System.Threading.Tasks;
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
   private readonly ScriptManager _script;

   public Main()
   {
      var (handyVm, filterVm, scriptVm) = ConfigSerializer.ReadFromFile();

      _handy = new HandyManager( handyVm );
      _script = new ScriptManager( scriptVm );

      _model = new MainWindowViewModel( handyVm, filterVm, scriptVm )
      {
         UploadScriptAndLaunchPlayerCommand = new RelayCommand<bool>( async forceUpload => await UploadScriptAndLaunchPlayerAsync( forceUpload ).ConfigureAwait( false ) )
      };

      _window = new MainWindow( _model );
   }

   public void Dispose()
   {
      ConfigSerializer.SaveToFile( _model.HandyVm, _model.FilterVm, _model.ScriptVm );
      _handy.Dispose();
      _vlc.Dispose();
   }

   public void Start() => _window.Show();

   private async Task UploadScriptAndLaunchPlayerAsync( bool forceUpload )
   {
      if ( !_script.VerifyPaths() )
      {
         Logger.Log( "Error: Could not find script or video file" );
         return;
      }

#if !TESTINGPLAYER
      if ( !await _handy.UploadScriptAsync( _model.ScriptVm.ScriptFilePath, forceUpload ).ConfigureAwait( true ) )
      {
         return;
      }
#endif

      _window.Hide();
      var videoPlayer = new VideoPlayerWindow( _vlc );
      videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _model.ScriptVm.VideoFilePath, _model.FilterVm );
      videoPlayer.Closing += ( _, _ ) => _ = ThreadPool.QueueUserWorkItem( _ => _vlc.CloseVideo() );

      using ( new VlcScriptSynchronizer( _vlc, _handy ) )
      {
         videoPlayer.ShowDialog();
      }

      await Task.Delay( 2000 ).ConfigureAwait( true ); // Give time to cleanup

      await _handy.SyncLocalRangeWithDeviceRangeAsync().ConfigureAwait( true );

      _window.Show();
      _window.Activate();
   }
}
