using System;
using System.Threading;
using System.Threading.Tasks;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal sealed class VlcScriptSynchronizer : IDisposable
{
   private readonly VlcManager _vlc;
   private readonly ScriptManager _scriptManager;
   private readonly HandyManager _handy;

   public VlcScriptSynchronizer( VlcManager vlc, ScriptManager scriptManager, HandyManager handy )
   {
      _vlc = vlc;

      _scriptManager = scriptManager;
      _scriptManager.ScriptChanged += OnScriptChanged;

      _handy = handy;
      if ( _handy.IsConnected )
      {
         _vlc.MediaOpened += OnMediaOpened;
         _vlc.MediaClosed += OnPlayerPausedOrClosed;
      }
   }

   public void Dispose()
   {
      var player = _vlc.Player;
      player.Playing -= OnPlayerPlaying;
      player.Paused -= OnPlayerPausedOrClosed;

      _vlc.MediaOpened -= OnMediaOpened;
      _vlc.MediaClosed -= OnPlayerPausedOrClosed;

      _scriptManager.ScriptChanged -= OnScriptChanged;
   }

   private async void OnScriptChanged( object sender, EventArgs e )
   {
      if ( !_handy.IsConnected )
      {
         return;
      }

      _vlc.SetPlaybackEnabled( false );
      _vlc.Marquee.SetPriorityText( "Sending updated script to devices..." );

      await _handy.UpdateScriptAsync( _scriptManager.Script );

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

   private void OnPlayerPlaying( object sender, EventArgs e ) => _ = ThreadPool.QueueUserWorkItem( async _ => await _handy.StartSyncAsync( (long)_vlc.TimeProvider.GetCurrentTime().TotalMilliseconds ) );

   private void OnPlayerPausedOrClosed( object sender, EventArgs e ) => _ = ThreadPool.QueueUserWorkItem( async _ => await _handy.StopSyncAsync() );

   public async Task<bool> SetupSyncAsync( Funscript script ) => await _handy.SetupSyncAsync( script );
}
