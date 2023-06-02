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
   private readonly VlcFilter _vlcFilter;

   private DateTime _lastPauseToggleTime = DateTime.MinValue;

   public VideoPlayerWindow( VlcManager vlc )
   {
      _hideScrubberTimer = new DispatcherTimer( TimeSpan.FromSeconds( 3 ), DispatcherPriority.Normal, OnHideScrubberTimerTick, Dispatcher ) { IsEnabled = false };
      _vlcFilter = vlc.Filter;

      InitializeComponent();

      VideoPlayer.Margin = new Thickness( ( SystemParameters.WindowResizeBorderThickness.Left * 2 ) - 1 );
      VideoPlayer.MediaPlayer = vlc.Player;

      UniversalClick.AddClickHandler( VideoClickHandler, OnVideoClick );
      InputManager.Current.PreProcessInput += OnInputManagerPreProcessInput;
      vlc.MediaSetupComplete += OnMediaSetupComplete;
   }

   private void OnMediaSetupComplete( object sender, EventArgs e )
   {
      var vlc = (VlcManager)sender;
      vlc.MediaSetupComplete -= OnMediaSetupComplete;

      VolumeOverlay.SetVlc( vlc );
      PlayPauseindicator.SetPlayer( vlc.Player );
      VideoControls.SetVlc( vlc );
   }

   private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
   {
      Mouse.OverrideCursor = null;
      _hideScrubberTimer.Stop();
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
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
            Close();
            break;
         }
         case Key.B when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
         {
            _vlcFilter.BaseBoostEnabled = !_vlcFilter.BaseBoostEnabled;
            break;
         }
         case Key.S when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
         {
            _vlcFilter.SaturationBoostEnabled = !_vlcFilter.SaturationBoostEnabled;
            break;
         }
      }
   }

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
      const int volumeIncrement = 5;

      var player = VideoPlayer.MediaPlayer;
      var volume = player.Volume;
      if ( e.Delta > 0 )
      {
         if ( volume == 100 )
         {
            _vlcFilter.VolumeAmpEnabled = true;
         }
         else
         {
            volume += volumeIncrement;
         }

         player.Volume = volume;
      }
      else if ( e.Delta < 0 )
      {
         if ( _vlcFilter.VolumeAmpEnabled )
         {
            _vlcFilter.VolumeAmpEnabled = false;
         }
         else
         {
            volume -= volumeIncrement;
         }

         player.Volume = volume;
      }
   }

   private void OnVideoClick( object sender, RoutedEventArgs e ) => ThrottledTogglePause();

   private void ThrottledTogglePause()
   {
      if ( DateTime.Now < _lastPauseToggleTime + TimeSpan.FromSeconds( 1 ) )
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
