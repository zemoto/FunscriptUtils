using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal sealed class Main : IDisposable
{
   private readonly MainWindow _window;
   private readonly MainViewModel _model;
   private readonly VlcManager _vlc;
   private readonly HandyManager _handy;
   private readonly ScriptManager _script;
   private readonly HotkeyManager _hotkeyManager;

   private bool _playerOpen;

   public Main()
   {
      _model = ConfigSerializer.ReadFromFile();
      _model.UploadScriptAndLaunchPlayerCommand = new RelayCommand( async () => await UploadScriptAndLaunchPlayerAsync(), () => !_playerOpen );

      var monitors = new List<string>();
      for ( int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++ )
      {
         monitors.Add( $"Monitor {i + 1}" );
      }
      _model.PlaybackVm.Monitors = monitors;

      _vlc = new VlcManager( _model.FilterVm, _model.PlaybackVm );
      _handy = new HandyManager( _model.HandyVm );
      _script = new ScriptManager( _model.ScriptVm );
      _hotkeyManager = new HotkeyManager( _vlc, _handy, _script );

      _window = new MainWindow( _model );
      _window.Closed += OnMainWindowClosed;
   }

   private void OnMainWindowClosed( object sender, EventArgs e )
   {
      Dispose();
      Application.Current.Shutdown();
   }

   public void Dispose()
   {
      ConfigSerializer.SaveToFile( _model );
      _handy.Dispose();
      _vlc.Dispose();
      _hotkeyManager.Dispose();
      _script.Dispose();
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
         using var synchronizer = new VlcScriptSynchronizer( _vlc, _script, _handy );
         if ( !await synchronizer.SetupSyncAsync( _model.ScriptVm.Script ).ConfigureAwait( true ) )
         {
            return;
         }

         _window.Hide();
         var videoPlayer = new VideoPlayerWindow( _vlc, _script, _model.PlaybackVm.SelectedMonitorIdx );
         videoPlayer.Loaded += ( _, _ ) => _vlc.OpenVideo( _model.ScriptVm.VideoFile.FullPath );

         _ = videoPlayer.ShowDialog();
      }

      _window.Show();
      _ = _window.Activate();
   }
}
