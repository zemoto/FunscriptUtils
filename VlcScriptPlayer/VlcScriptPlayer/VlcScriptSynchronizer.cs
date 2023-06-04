using System;
using System.Windows;
using System.Windows.Threading;
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

      _ = _api.StopScriptAsync();
   }

   private void OnMediaSetupComplete( object sender, EventArgs e )
   {
      _vlc.MediaSetupComplete -= OnMediaSetupComplete;

      var player = _vlc.Player;
      player.Playing += OnPlayerPlaying;
      player.Paused += OnPlayStoppedOrPaused;
      player.Stopped += OnPlayStoppedOrPaused;
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _ = Application.Current.Dispatcher.BeginInvoke( async () => await _api.PlayScriptAsync( _vlc.Player.Time ), DispatcherPriority.Send );

   private void OnPlayStoppedOrPaused( object sender, EventArgs e ) => _ = Application.Current.Dispatcher.BeginInvoke( async () => await _api.StopScriptAsync(), DispatcherPriority.Send );
}
