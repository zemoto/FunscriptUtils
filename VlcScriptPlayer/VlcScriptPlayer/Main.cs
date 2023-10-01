using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VlcScriptPlayer.Config;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Main : IDisposable
{
   private readonly MainWindow _window;
   private readonly MainWindowViewModel _model;
   private readonly AppConfig _config = AppConfig.ReadFromFile();
   private readonly HandyApi _handyApi = new();
   private readonly VlcManager _vlc = new();

   public Main()
   {
      _model = new MainWindowViewModel( _config )
      {
         ConnectCommand = new RelayCommand( () => _ = ConnectToHandyAsync() ),
         SetOffsetCommand = new RelayCommand( () => _ = SetHandyOffsetAsync() ),
         SetRangeCommand = new RelayCommand( () => _ = SetHandyRangeAsync() ),
         SelectVideoCommand = new RelayCommand( SelectVideo ),
         SelectScriptCommand = new RelayCommand( SelectScript ),
         SelectScriptFolderCommand = new RelayCommand( SelectScriptFolder ),
         UploadScriptAndLaunchPlayerCommand = new RelayCommand( async () => await UploadScriptAndLaunchPlayerAsync().ConfigureAwait( false ) )
      };

      _window = new MainWindow( _model );
   }

   public void Dispose()
   {
      _config.SaveToFile();
      _handyApi.Dispose();
      _vlc.Dispose();
   }

   public void Start() => _window.Show();

   private async Task ConnectToHandyAsync()
   {
#if !TESTINGPLAYER
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _config.Handy.IsConnected = false;
      if ( !await _handyApi.ConnectToAndSetupHandyAsync( _config.Handy ).ConfigureAwait( false ) )
      {
         return;
      }

      _config.Handy.CurrentOffset = _config.Handy.DesiredOffset;
      _config.Handy.CurrentSlideMin = _config.Handy.DesiredSlideMin;
      _config.Handy.CurrentSlideMax = _config.Handy.DesiredSlideMax;
#endif

      _config.Handy.IsConnected = true;
   }

   private async Task SetHandyOffsetAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetOffsetAsync( _config.Handy.DesiredOffset ).ConfigureAwait( false ) )
      {
         _config.Handy.CurrentOffset = _config.Handy.DesiredOffset;
      }
   }

   private async Task SetHandyRangeAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetRangeAsync( _config.Handy.DesiredSlideMin, _config.Handy.DesiredSlideMax ).ConfigureAwait( false ) )
      {
         _config.Handy.CurrentSlideMin = _config.Handy.DesiredSlideMin;
         _config.Handy.CurrentSlideMax = _config.Handy.DesiredSlideMax;
      }
   }

   private void SelectVideo()
   {
      const string filter = "Video Files (*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi)|*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi|All files (*.*)|*.*";
      var dlg = new VistaOpenFileDialog
      {
         Filter = filter,
         Multiselect = false
      };

      if ( dlg.ShowDialog( _window ) != true )
      {
         return;
      }

      _config.VideoFilePath = dlg.FileName;

      var videoFolderPath = Path.GetDirectoryName( dlg.FileName );
      var fileName = Path.GetFileNameWithoutExtension( dlg.FileName );
      foreach ( var folder in new string[2] { videoFolderPath, _config.ScriptFolder } )
      {
         if ( string.IsNullOrEmpty( folder ) )
         {
            continue;
         }

         HandyLogger.Log( $"Searching folder for script: {folder}" );
         var scripts = Directory.GetFiles( folder, "*.funscript" ).Concat( Directory.GetFiles( folder, "*.csv" ) ).ToArray();
         var matchingScript = Array.Find( scripts, x => Path.GetFileNameWithoutExtension( x ).Equals( fileName, StringComparison.Ordinal ) );
         if ( !string.IsNullOrWhiteSpace( matchingScript ) )
         {
            _config.ScriptFilePath = matchingScript;
            HandyLogger.Log( $"Found script: {matchingScript}" );
            return;
         }
      }
   }

   private void SelectScript()
   {
      const string filter = "Script Files (*.funscript;*.csv)|*.funscript;*.csv|All files (*.*)|*.*";
      var dlg = new VistaOpenFileDialog
      {
         Filter = filter,
         Multiselect = false
      };

      if ( dlg.ShowDialog( _window ) == true )
      {
         _config.ScriptFilePath = dlg.FileName;
      }
   }

   private void SelectScriptFolder()
   {
      var dlg = new VistaFolderBrowserDialog() { Multiselect = false };
      if ( dlg.ShowDialog( _window ) == true )
      {
         _config.ScriptFolder = dlg.SelectedPath;
      }
   }

   private async Task UploadScriptAndLaunchPlayerAsync()
   {
      _model.RequestInProgress = true;

#if !TESTINGPLAYER
      using ( new ScopeGuard( () => _model.RequestInProgress = false ) )
      {
         if ( !await _handyApi.UploadScriptAsync( _config.ScriptFilePath, _config.ForceUploadScript ).ConfigureAwait( true ) )
         {
            return;
         }
      }
#endif

      _window.Hide();
      var videoPlayer = new VideoPlayerWindow( _vlc );
      videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _config.VideoFilePath, _config.Filters );
      videoPlayer.Closing += ( _, _ ) => _ = ThreadPool.QueueUserWorkItem( _ => _vlc.CloseVideo() );

      using ( new VlcScriptSynchronizer( _vlc, _handyApi ) )
      {
         videoPlayer.ShowDialog();
      }

      await Task.Delay( 2000 ).ConfigureAwait( true ); // Give time to cleanup

      (_config.Handy.CurrentSlideMin, _config.Handy.CurrentSlideMax) = await _handyApi.GetRangeAsync().ConfigureAwait( true );

      _window.Show();
      _window.Activate();
   }
}
