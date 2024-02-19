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
   private readonly Funscript _script;

   public VideoPlayerWindow( VlcManager vlc, Funscript script )
   {
      _hideScrubberTimer = new DispatcherTimer( TimeSpan.FromSeconds( 3 ), DispatcherPriority.Normal, OnHideScrubberTimerTick, Dispatcher ) { IsEnabled = false };
      _vlc = vlc;
      _script = script;

      InitializeComponent();

      VideoPlayer.Margin = new Thickness( ( SystemParameters.WindowResizeBorderThickness.Left * 2 ) - 1 );
      VideoPlayer.MediaPlayer = vlc.Player;

      vlc.MediaOpened += OnMediaOpened;
   }

   private void OnMediaOpened( object sender, EventArgs e )
   {
      _vlc.MediaOpened -= OnMediaOpened;
      Dispatcher.Invoke( () =>
      {
         MarqueeOverlay.Init( _vlc );
         PlayPauseindicator.Init( _vlc.Player );
         VideoControls.Init( _vlc, _script );

         _vlc.MediaClosed += OnMediaClosed;
         MouseEventGrid.MouseWheel += OnMouseWheel;
         UniversalClick.AddClickHandler( VideoClickHandler, OnVideoClick );
      } );
   }

   private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
   {
      Mouse.OverrideCursor = null;
      _hideScrubberTimer.Stop();
      _vlc.MediaClosed -= OnMediaClosed;
   }

   private void OnMediaClosed( object sender, EventArgs e ) => Dispatcher.Invoke( Close );

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
      if ( e.Delta > 0 )
      {
         _vlc.VolumeManager.IncrementVolume();
      }
      else if ( e.Delta < 0 )
      {
         _vlc.VolumeManager.DecrementVolume();
      }
   }

   private void OnVideoClick( object sender, RoutedEventArgs e ) => _vlc.TogglePlayPause();
}
