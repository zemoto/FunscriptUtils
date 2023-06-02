using System;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal sealed class VlcScriptSynchronizer : IDisposable
{
   private readonly VlcManager _vlc;
   private readonly HandyApi _api;

   public VlcScriptSynchronizer( VlcManager vlc, HandyApi api )
   {
      _vlc = vlc;
      _api = api;

      _vlc.MediaSetupComplete += OnMediaSetupComplete;
   }

   public void Dispose()
   {
      var player = _vlc.Player;
      player.Playing -= OnPlayerPlaying;
      player.Paused -= OnPlayStoppedOrPaused;
      player.Stopped -= OnPlayStoppedOrPaused;
   }

   private void OnMediaSetupComplete( object sender, EventArgs e )
   {
      _vlc.MediaSetupComplete -= OnMediaSetupComplete;

      var player = _vlc.Player;
      player.Playing += OnPlayerPlaying;
      player.Paused += OnPlayStoppedOrPaused;
      player.Stopped += OnPlayStoppedOrPaused;
   }

   // Fire and forget async. We don't care what thread they are executed on and don't care to wait for the return value.
   private void OnPlayerPlaying( object sender, EventArgs e ) => _ = _api.PlayScriptAsync( _vlc.Player.Time );

   private void OnPlayStoppedOrPaused( object sender, EventArgs e ) => _ = _api.StopScriptAsync();
}
