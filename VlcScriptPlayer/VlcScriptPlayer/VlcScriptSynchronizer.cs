using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal interface ISyncTarget
{
   bool CanSync { get; }

   Task<bool> SetupSyncAsync( Funscript script );
   Task StartSyncAsync( long time );
   Task StopSyncAsync();
   Task CleanupAsync();
}

internal sealed class VlcScriptSynchronizer : IAsyncDisposable
{
   private readonly VlcManager _vlc;
   private readonly List<ISyncTarget> _syncTargets;

   public VlcScriptSynchronizer( VlcManager vlc, params ISyncTarget[] syncTargets )
   {
      _vlc = vlc;
      _syncTargets = syncTargets.Where( x => x.CanSync ).ToList();

      if ( _syncTargets.Any() )
      {
         _vlc.MediaSetupComplete += OnMediaSetupComplete;
      }
   }

   public async ValueTask DisposeAsync()
   {
      var player = _vlc.Player;
      player.Playing -= OnPlayerPlaying;
      player.Paused -= OnPlayStoppedOrPaused;
      player.Stopped -= OnPlayStoppedOrPaused;

      foreach ( var syncTarget in _syncTargets )
      {
         await syncTarget.CleanupAsync();
      }
   }

   private void OnMediaSetupComplete( object sender, EventArgs e )
   {
      _vlc.MediaSetupComplete -= OnMediaSetupComplete;

      var player = _vlc.Player;
      player.Playing += OnPlayerPlaying;
      player.Paused += OnPlayStoppedOrPaused;
      player.Stopped += OnPlayStoppedOrPaused;
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _ = Application.Current.Dispatcher.BeginInvoke( async () => await StartSyncAsync(), DispatcherPriority.Send );

   private void OnPlayStoppedOrPaused( object sender, EventArgs e ) => _ = Application.Current.Dispatcher.BeginInvoke( async () => await StopSyncAsync(), DispatcherPriority.Send );

   public async Task<bool> SetupSyncAsync( string scriptFilePath )
   {
      var script = Funscript.Load( scriptFilePath );
      foreach ( var syncTarget in _syncTargets )
      {
         if ( !await syncTarget.SetupSyncAsync( script ) )
         {
            return false;
         }
      }

      return true;
   }

   private async Task StartSyncAsync()
   {
      foreach ( var syncTarget in _syncTargets )
      {
         await syncTarget.StartSyncAsync( _vlc.Player.Time );
      }
   }

   private async Task StopSyncAsync()
   {
      foreach ( var syncTarget in _syncTargets )
      {
         await syncTarget.StopSyncAsync();
      }
   }
}
