using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class Main : IDisposable
{
   private readonly Config _config;

   private readonly MainWindow _window;
   private readonly MainWindowViewModel _model;
   private readonly HandyApi _handyApi = new();

   private readonly VlcManager _vlc = new();

   public Main( Config config )
   {
      _config = config;

      _model = new MainWindowViewModel( _config )
      {
         ConnectCommand = new RelayCommand( async () => await ConnectToHandyAsync() ),
         SetOffsetCommand = new RelayCommand( async () => await SetHandyOffsetAsync( _model.DesiredOffset ) ),
         SelectVideoCommand = new RelayCommand( SelectVideo ),
         SelectScriptCommand = new RelayCommand( SelectScript ),
         AddScriptFolderCommand = new RelayCommand( AddScriptFolder ),
         RemoveScriptFolderCommand = new RelayCommand( RemoveScriptFolder ),
         UploadScriptCommand = new RelayCommand( async () => await UploadScriptAsync() )
      };

      _window = new MainWindow( _model );
   }

   public void Dispose()
   {
      _handyApi.Dispose();
      _vlc.Dispose();
   }

   public void Start() => _window.Show();

   private async Task ConnectToHandyAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _model.IsConnected = false;
      _handyApi.SetConnectionId( _model.ConnectionId );
      if ( !await _handyApi.ConnectAsync() ||
           !await _handyApi.SetupServerClockSyncAsync() ||
           !await _handyApi.EnsureModeAsync() )
      {
         return;
      }

      if ( _config.DesiredOffset != 0 )
      {
         await SetHandyOffsetAsync( _config.DesiredOffset );
      }
      else
      {
         _model.CurrentOffset = await _handyApi.GetOffsetAsync();
      }

      _config.ConnectionId = _model.ConnectionId;
      _model.IsConnected = true;
   }

   private async Task SetHandyOffsetAsync( int desiredOffset )
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _config.DesiredOffset = desiredOffset;
      if ( await _handyApi.SetOffsetAsync( desiredOffset ) )
      {
         _model.CurrentOffset = desiredOffset;
      }
   }

   private void SelectVideo()
   {
      var dlg = new VistaOpenFileDialog
      {
         Filter = "Video Files (*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi)|*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi|All files (*.*)|*.*",
         Multiselect = false
      };

      if ( dlg.ShowDialog( _window ) != true )
      {
         return;
      }

      _model.VideoFilePath = dlg.FileName;

      var videoFolderPath = Path.GetDirectoryName( dlg.FileName );
      var folders = _model.ScriptFolders.ToList();
      if ( !folders.Contains( videoFolderPath ) )
      {
         folders.Insert( 0, videoFolderPath );
      }

      var fileName = Path.GetFileNameWithoutExtension( dlg.FileName );
      foreach ( var folder in folders )
      {
         HandyLogger.Log( $"Searching folder for script: {folder}" );
         var scripts = Directory.GetFiles( folder, "*.funscript" ).Concat( Directory.GetFiles( folder, "*.csv" ) ).ToArray();
         var matchingScript = Array.Find( scripts, x => Path.GetFileNameWithoutExtension( x ).Equals( fileName, StringComparison.InvariantCultureIgnoreCase ) );
         if ( !string.IsNullOrWhiteSpace( matchingScript ) )
         {
            _model.ScriptFilePath = matchingScript;
            HandyLogger.Log( $"Found script: {matchingScript}" );
            return;
         }
      }
   }

   private void SelectScript()
   {
      var dlg = new VistaOpenFileDialog
      {
         Filter = "Script Files (*.funscript;*.csv)|*.funscript;*.csv|All files (*.*)|*.*",
         Multiselect = false
      };

      if ( dlg.ShowDialog( _window ) == true )
      {
         _model.ScriptFilePath = dlg.FileName;
      }
   }

   private void AddScriptFolder()
   {
      var dlg = new VistaFolderBrowserDialog();
      if ( dlg.ShowDialog( _window ) == true )
      {
         _model.ScriptFolders.Add( dlg.SelectedPath );
         _config.ScriptFolders.Add( dlg.SelectedPath );
      }
   }

   private void RemoveScriptFolder()
   {
      _config.ScriptFolders.Remove( _model.SelectedScriptFilePath );
      _model.ScriptFolders.Remove( _model.SelectedScriptFilePath );
   }

   private async Task UploadScriptAsync()
   {
      _model.RequestInProgress = true;
      using ( new ScopeGuard( () => _model.RequestInProgress = false ) )
      {
         if ( !await _handyApi.UploadScriptAsync( _model.ScriptFilePath ) )
         {
            return;
         }
      }

      LaunchPlayer();
   }

   private void LaunchPlayer()
   {
      _window.Hide();

      var videoPlayer = new VideoPlayerWindow( _vlc );
      using ( new VlcScriptSynchronizer( _vlc, _handyApi ) )
      {
         _vlc.OpenVideo( _model.VideoFilePath, _model );
         videoPlayer.ShowDialog();
         _ = ThreadPool.QueueUserWorkItem( _ => _vlc.Player.Stop() );
      }

      _window.Show();
   }
}
