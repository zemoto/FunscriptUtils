using LibVLCSharp.Shared;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal partial class VideoControls
{
   public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register( nameof( Player ), typeof( MediaPlayer ), typeof( VideoControls ), new PropertyMetadata( null, OnPlayerChanged ) );
   public MediaPlayer Player
   {
      get => (MediaPlayer)GetValue( PlayerProperty );
      set => SetValue( PlayerProperty, value );
   }
   private static void OnPlayerChanged( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( (VideoControls)d ).OnPlayerChanged();
   private void OnPlayerChanged()
   {
      Player.Playing += OnPlayerPlaying;
      Player.Paused += OnPlayerPaused;
      Player.Stopped += OnPlayerStopped;
      Dispatcher.BeginInvoke( () =>
      {
         _videoDuration = TimeSpan.FromMilliseconds( Player.Media.Duration );
         DurationLabel.Text = TimeSpanToString( _videoDuration );
         CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.Zero );
      } );
   }

   private readonly DispatcherTimer _playbackTimer;

   private TimeSpan _videoDuration;

   public VideoControls()
   {
      _playbackTimer = new DispatcherTimer( TimeSpan.FromSeconds( 1 ), DispatcherPriority.Normal, OnPlaybackTimerTick, Dispatcher ) { IsEnabled = false };

      InitializeComponent();

      UniversalClick.AddClickHandler( PositionTrack, OnTrackClicked );
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      var player = Player;
      if ( player is null )
      {
         return;
      }

      player.Playing -= OnPlayerPlaying;
      player.Paused -= OnPlayerPaused;
      player.Stopped -= OnPlayerStopped;
   }

   private void OnPlaybackTimerTick( object sender, EventArgs e )
   {
      UpdateCurrentTimeLabel();
      SetTrackProgress( Player.Position );
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _playbackTimer.Start();

   private void OnPlayerPaused( object sender, EventArgs e ) => _playbackTimer.Stop();

   private void OnPlayerPositionChanged( object sender, MediaPlayerPositionChangedEventArgs e )
   {
      Dispatcher.BeginInvoke( () =>
      {
         var time = TimeSpan.FromMilliseconds( Player.Time );
         CurrentTimeLabel.Text = TimeSpanToString( time );

         SetTrackProgress( Player.Position );
      } );
   }

   private void OnPlayerStopped( object sender, EventArgs e )
   {
      Dispatcher.BeginInvoke( () =>
      {
         CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.Zero );
         SetTrackProgress( 0 );
      } );
   }

   private string TimeSpanToString( TimeSpan time ) => _videoDuration.TotalHours >= 1 ? time.ToString( @"h\:mm\:ss" ) : time.ToString( @"m\:ss" );

   private void SetTrackProgress( double value )
   {
      value = Math.Clamp( value, 0, 100 );
      TrackIndicator.Width = value * PositionTrack.ActualWidth;
   }

   private void UpdateCurrentTimeLabel() => CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.FromMilliseconds( Player.Time ) );

   private void OnScrubberMouseEnter( object sender, MouseEventArgs e )
   {
      TrackContainer.Opacity = 1;
      TrackContainer.Height = 4;
      TrackPreview.Visibility = Visibility.Visible;
      TimePreview.Visibility = Visibility.Visible;
   }

   private void OnScrubberMouseLeave( object sender, MouseEventArgs e )
   {
      TrackContainer.Opacity = 0.72;
      TrackContainer.Height = 3;
      TrackPreview.Visibility = Visibility.Collapsed;
      TimePreview.Visibility = Visibility.Collapsed;
   }

   private void OnScrubberMouseMove( object sender, MouseEventArgs e )
   {
      TrackPreview.Width = Math.Clamp( Mouse.GetPosition( TrackContainer ).X, 0, TrackContainer.ActualWidth );

      TimePreviewText.Text = TimeSpanToString( TrackPreview.Width / PositionTrack.ActualWidth * _videoDuration );
      Canvas.SetLeft( TimePreview, TrackPreview.Width - ( TimePreview.ActualWidth / 2 ) );
   }

   private async void OnTrackClicked( object sender, RoutedEventArgs e )
   {
      var newPosition = (float)( TrackPreview.Width / PositionTrack.ActualWidth );

      // Snap to the beginning if clicking early enough
      if ( newPosition < 0.03 )
      {
         newPosition = 0;
      }

      if ( _playbackTimer.IsEnabled )
      {
         Player.SetPause( true );
         await Task.Delay( 200 );
      }

      Player.Time = (long)( newPosition * _videoDuration.TotalMilliseconds );

      SetTrackProgress( newPosition );
      UpdateCurrentTimeLabel();
   }
}
