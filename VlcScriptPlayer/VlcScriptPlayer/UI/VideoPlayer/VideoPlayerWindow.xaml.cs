using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VlcScriptPlayer.Vlc;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class VideoPlayerWindow
{
   private readonly DispatcherTimer _hideScrubberTimer;
   private readonly VlcManager _vlc;

   private bool _mediaReady;

   private DateTime _lastPauseToggleTime = DateTime.MinValue;

   public VideoPlayerWindow( VlcManager vlc )
   {
      _hideScrubberTimer = new DispatcherTimer( TimeSpan.FromSeconds( 3 ), DispatcherPriority.Normal, OnHideScrubberTimerTick, Dispatcher ) { IsEnabled = false };
      _vlc = vlc;

      InitializeComponent();

      VideoPlayer.Margin = new Thickness( ( SystemParameters.WindowResizeBorderThickness.Left * 2 ) - 1 );
      VideoPlayer.MediaPlayer = vlc.Player;

      UniversalClick.AddClickHandler( VideoClickHandler, OnVideoClick );
      InputManager.Current.PreProcessInput += OnInputManagerPreProcessInput;
      vlc.MediaSetupComplete += OnMediaSetupComplete;
   }

   private void OnMediaSetupComplete( object sender, EventArgs e )
   {
      _vlc.MediaSetupComplete -= OnMediaSetupComplete;

      VolumeOverlay.Init( _vlc );
      PlayPauseindicator.Init( _vlc.Player );
      VideoControls.Init( _vlc );

      _vlc.Player.EndReached += OnPlayerEndReached;
      _mediaReady = true;
   }

   private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
   {
      Mouse.OverrideCursor = null;
      _hideScrubberTimer.Stop();
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
      _vlc.Player.EndReached -= OnPlayerEndReached;
   }

   private void OnInputManagerPreProcessInput( object sender, PreProcessInputEventArgs e )
   {
      if ( e?.StagingItem?.Input is not KeyEventArgs keyEvent || keyEvent.RoutedEvent != Keyboard.KeyDownEvent )
      {
         return;
      }

      switch ( keyEvent.Key )
      {
         case Key.Space:
         {
            ThrottledTogglePause();
            break;
         }
         case Key.Escape:
         {
            if ( _mediaReady )
            {
               Close();
            }
            break;
         }
         case Key.B when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
         {
            _vlc.Filter.BassBoostEnabled = !_vlc.Filter.BassBoostEnabled;
            break;
         }
         case Key.S when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
         {
            _vlc.Filter.SaturationBoostEnabled = !_vlc.Filter.SaturationBoostEnabled;
            break;
         }
      }
   }

   private void OnPlayerEndReached( object sender, EventArgs e ) => Dispatcher.BeginInvoke( Close );

   private void OnMouseMoveOverVideo( object sender, MouseEventArgs e )
   {
      Mouse.OverrideCursor = null;
      VideoControls.Visibility = Visibility.Visible;
      _hideScrubberTimer.Stop();
      _hideScrubberTimer.Start();
   }

   private void OnHideScrubberTimerTick( object sender, EventArgs e )
   {
      _hideScrubberTimer.Stop();
      if ( !VideoControls.IsMouseOver )
      {
         VideoControls.Visibility = Visibility.Collapsed;
         Mouse.OverrideCursor = Cursors.None;
      }
   }

   private void OnMouseLeavingVideo( object sender, MouseEventArgs e )
   {
      Mouse.OverrideCursor = null;
      _hideScrubberTimer.Stop();
      VideoControls.Visibility = Visibility.Collapsed;
   }

   private void OnMouseWheel( object sender, MouseWheelEventArgs e )
   {
      if ( !_mediaReady )
      {
         return;
      }

      if ( e.Delta > 0 )
      {
         _vlc.VolumeManager.IncrementVolume();
      }
      else if ( e.Delta < 0 )
      {
         _vlc.VolumeManager.DecrementVolume();
      }
   }

   private void OnVideoClick( object sender, RoutedEventArgs e ) => ThrottledTogglePause();

   private void ThrottledTogglePause()
   {
      if ( !_mediaReady || DateTime.Now < _lastPauseToggleTime + TimeSpan.FromSeconds( 1 ) )
      {
         return;
      }

      var player = VideoPlayer.MediaPlayer;
      if ( player.CanPause )
      {
         player.Pause();
      }
      else
      {
         player.Play();
      }

      _lastPauseToggleTime = DateTime.Now;
   }
}
