using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal interface ISyncTarget
{
   bool CanSync { get; }

   Task<bool> SetupSyncAsync( Funscript script );
   Task UpdateScriptAsync( Funscript script );
   Task StartSyncAsync( long time );
   Task StopSyncAsync();
   Task CleanupAsync( bool syncSetupSuccessful );
}

internal sealed class VlcScriptSynchronizer : IAsyncDisposable
{
   private readonly VlcManager _vlc;
   private readonly ScriptManager _scriptManager;
   private readonly List<ISyncTarget> _syncTargets;

   private bool _syncSetupSuccessful;

   public VlcScriptSynchronizer( VlcManager vlc, ScriptManager scriptManager, params ISyncTarget[] syncTargets )
   {
      _vlc = vlc;

      _scriptManager = scriptManager;
      _scriptManager.ScriptChanged += OnScriptChanged;

      _syncTargets = syncTargets.Where( x => x.CanSync ).ToList();
      if ( _syncTargets.Count > 0 )
      {
         _vlc.MediaOpened += OnMediaOpened;
         _vlc.MediaClosed += OnPlayerPausedOrClosed;
      }
   }

   public async ValueTask DisposeAsync()
   {
      var player = _vlc.Player;
      player.Playing -= OnPlayerPlaying;
      player.Paused -= OnPlayerPausedOrClosed;

      _vlc.MediaOpened -= OnMediaOpened;
      _vlc.MediaClosed -= OnPlayerPausedOrClosed;

      _scriptManager.ScriptChanged -= OnScriptChanged;

      foreach ( var syncTarget in _syncTargets )
      {
         await syncTarget.CleanupAsync( _syncSetupSuccessful );
      }
   }

   private async void OnScriptChanged( object sender, EventArgs e )
   {
      if ( _syncTargets.Count == 0 )
      {
         return;
      }

      _vlc.SetPlaybackEnabled( false );
      _vlc.Marquee.SetPriorityText( "Sending updated script to devices..." );

      foreach ( var syncTarget in _syncTargets )
      {
         await syncTarget.UpdateScriptAsync( _scriptManager.Model.Script );
      }

      _vlc.Marquee.FinalizePriorityText( "Updated script synced" );
      _vlc.SetPlaybackEnabled( true );
   }

   private void OnMediaOpened( object sender, EventArgs e )
   {
      _vlc.MediaOpened -= OnMediaOpened;

      var player = _vlc.Player;
      player.Playing += OnPlayerPlaying;
      player.Paused += OnPlayerPausedOrClosed;
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _ = ThreadPool.QueueUserWorkItem( async _ => await StartSyncAsync() );

   private void OnPlayerPausedOrClosed( object sender, EventArgs e ) => _ = ThreadPool.QueueUserWorkItem( async _ => await StopSyncAsync() );

   public async Task<bool> SetupSyncAsync( Funscript script )
   {
      foreach ( var syncTarget in _syncTargets )
      {
         if ( !await syncTarget.SetupSyncAsync( script ) )
         {
            return false;
         }
      }

      _syncSetupSuccessful = true;
      return true;
   }

   private async Task StartSyncAsync()
   {
      var time = (long)_vlc.TimeProvider.GetCurrentTime().TotalMilliseconds;
      await Task.WhenAll( _syncTargets.Select( x => x.StartSyncAsync( time ) ) );
   }

   private async Task StopSyncAsync() => await Task.WhenAll( _syncTargets.Select( x => x.StopSyncAsync() ) );
}
