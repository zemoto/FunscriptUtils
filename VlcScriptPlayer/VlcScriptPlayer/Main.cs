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
         SetOffsetCommand = new RelayCommand( () => _ = SetHandyOffsetAsync( _model.Config.Handy.DesiredOffset ) ),
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
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _config.Handy.IsConnected = false;
      _handyApi.SetConnectionId( _config.Handy.ConnectionId );
      if ( !await _handyApi.ConnectAsync().ConfigureAwait( false ) ||
           !await _handyApi.SetupServerClockSyncAsync().ConfigureAwait( false ) ||
           !await _handyApi.EnsureModeAsync().ConfigureAwait( false ) )
      {
         return;
      }

      if ( _config.Handy.DesiredOffset != 0 )
      {
         await SetHandyOffsetAsync( _config.Handy.DesiredOffset ).ConfigureAwait( false );
      }
      else
      {
         _config.Handy.CurrentOffset = await _handyApi.GetOffsetAsync().ConfigureAwait( false );
      }

      _config.Handy.IsConnected = true;
   }

   private async Task SetHandyOffsetAsync( int desiredOffset )
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetOffsetAsync( desiredOffset ).ConfigureAwait( false ) )
      {
         _config.Handy.CurrentOffset = desiredOffset;
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
      using ( new ScopeGuard( () => _model.RequestInProgress = false ) )
      {
         if ( !await _handyApi.UploadScriptAsync( _config.ScriptFilePath, _config.ForceUploadScript ).ConfigureAwait( true ) )
         {
            return;
         }
      }

      _window.Hide();
      var videoPlayer = new VideoPlayerWindow( _vlc );
      videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _config.VideoFilePath, _config.Filters );
      videoPlayer.Closing += ( _, _ ) => _ = ThreadPool.QueueUserWorkItem( _ => _vlc.CloseVideo() );

      using ( new VlcScriptSynchronizer( _vlc, _handyApi ) )
      {
         videoPlayer.ShowDialog();
      }

      await Task.Delay( 2000 ).ConfigureAwait( true ); // Give time to cleanup

      _window.Show();
      _window.Activate();
   }
}
