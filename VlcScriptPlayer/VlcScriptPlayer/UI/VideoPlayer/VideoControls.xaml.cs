using LibVLCSharp.Shared;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VlcScriptPlayer.Vlc;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class VideoControls
{
   private readonly DispatcherTimer _playbackTimer;

   private MediaPlayer _player;
   private VlcFilter _filter;
   private TimeSpan _videoDuration;

   public VideoControls()
   {
      _playbackTimer = new DispatcherTimer( TimeSpan.FromMilliseconds( 500 ), DispatcherPriority.Normal, OnPlaybackTimerTick, Dispatcher ) { IsEnabled = false };

      InitializeComponent();

      UniversalClick.AddClickHandler( PositionTrack, OnTrackClicked );
   }

   public void SetVlc( VlcManager vlc )
   {
      _player = vlc.Player;
      _player.Playing += OnPlayerPlaying;
      _player.Paused += OnPlayerPaused;
      _player.Stopped += OnPlayerStopped;

      _filter = vlc.Filter;
      _filter.PropertyChanged += OnFilterPropertyChanged;

      _videoDuration = TimeSpan.FromMilliseconds( _player.Media.Duration );
      DurationLabel.Text = TimeSpanToString( _videoDuration );
      CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.Zero );
      UpdateFilterState();
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      if ( _player is not null )
      {
         _player.Playing -= OnPlayerPlaying;
         _player.Paused -= OnPlayerPaused;
         _player.Stopped -= OnPlayerStopped;
      }

      if ( _filter is not null )
      {
         _filter.PropertyChanged -= OnFilterPropertyChanged;
      }
   }

   private void OnPlaybackTimerTick( object sender, EventArgs e )
   {
      UpdateCurrentTimeLabel();
      SetTrackProgress( _player.Position );
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _playbackTimer.Start();

   private void OnPlayerPaused( object sender, EventArgs e ) => _playbackTimer.Stop();

   private void OnPlayerStopped( object sender, EventArgs e )
   {
      _playbackTimer.Stop();
      Dispatcher.BeginInvoke( () =>
      {
         CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.Zero );
         SetTrackProgress( 0 );
      } );
   }

   private void OnFilterPropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e ) => UpdateFilterState();

   private void UpdateFilterState()
   {
      var filterString = new StringBuilder( 3 );
      if ( _filter.VolumeAmpEnabled )
      {
         filterString.Append( 'V' );
      }
      if ( _filter.SaturationBoostEnabled )
      {
         filterString.Append( 'S' );
      }
      if ( _filter.BassBoostEnabled )
      {
         filterString.Append( 'B' );
      }
      FilterIndicator.Text = filterString.ToString();
   }

   private string TimeSpanToString( TimeSpan time ) => time.ToString( _videoDuration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture );

   private void SetTrackProgress( float value )
   {
      value = Math.Clamp( value, 0f, 1f );
      TrackIndicator.Width = value * PositionTrack.ActualWidth;
   }

   private void UpdateCurrentTimeLabel() => CurrentTimeLabel.Text = TimeSpanToString( TimeSpan.FromMilliseconds( _player.Time ) );

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
         _player.SetPause( true );
         await Task.Delay( 200 ).ConfigureAwait( true );
      }

      _player.Time = (long)( newPosition * _videoDuration.TotalMilliseconds );

      SetTrackProgress( newPosition );
      UpdateCurrentTimeLabel();
   }
}
