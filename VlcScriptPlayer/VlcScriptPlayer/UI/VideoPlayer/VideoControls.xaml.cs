using LibVLCSharp.Shared;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class VideoControls
{
   private readonly DispatcherTimer _playbackTimer;

   private MediaPlayer _player;
   private VlcFilter _filter;
   private VlcTimeProvider _timeProvider;

   public VideoControls()
   {
      _playbackTimer = new DispatcherTimer( TimeSpan.FromMilliseconds( 500 ), DispatcherPriority.Normal, OnPlaybackTimerTick, Dispatcher ) { IsEnabled = false };
      InitializeComponent();
   }

   public void Init( VlcManager vlc )
   {
      _player = vlc.Player;
      _player.Playing += OnPlayerPlaying;
      _player.Paused += OnPlayerPaused;

      _filter = vlc.Filter;
      _filter.PropertyChanged += OnFilterPropertyChanged;

      _timeProvider = vlc.TimeProvider;

      DurationLabel.Text = _timeProvider.GetDurationString();
      CurrentTimeLabel.Text = _timeProvider.GetTimeStringAtPosition( 0 );
      TimeLabelContainer.Visibility = Visibility.Visible;
      UpdateFilterState();

      PositionTrack.MouseEnter += OnScrubberMouseEnter;
      PositionTrack.MouseLeave += OnScrubberMouseLeave;
      PositionTrack.MouseMove += OnScrubberMouseMove;
      UniversalClick.AddClickHandler( PositionTrack, OnTrackClicked );
   }

   public void SetScript( Funscript script )
   {
      if ( script is not null && _timeProvider is not null )
      {
         Heatmap.Fill = HeatmapGenerator.GetHeatmapBrush( script, _timeProvider.Duration.TotalMilliseconds );
      }
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      _playbackTimer.Stop();

      if ( _player is not null )
      {
         _player.Playing -= OnPlayerPlaying;
         _player.Paused -= OnPlayerPaused;
      }

      if ( _filter is not null )
      {
         _filter.PropertyChanged -= OnFilterPropertyChanged;
      }
   }

   private void OnPlaybackTimerTick( object sender, EventArgs e )
   {
      CurrentTimeLabel.Text = _timeProvider.GetCurrentTimeString();
      SetTrackProgress( _timeProvider.GetCurrentPlaybackPosition() );
   }

   private void OnPlayerPlaying( object sender, EventArgs e ) => _playbackTimer.Start();

   private void OnPlayerPaused( object sender, EventArgs e ) => _playbackTimer.Stop();

   private void OnFilterPropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e ) => UpdateFilterState();

   private void UpdateFilterState()
   {
      var filterString = new StringBuilder( 3 );
      if ( _filter.VolumeAmpEnabled )
      {
         _ = filterString.Append( 'V' );
      }
      if ( _filter.SaturationBoostEnabled )
      {
         _ = filterString.Append( 'S' );
      }
      if ( _filter.BassBoostEnabled )
      {
         _ = filterString.Append( 'B' );
      }
      FilterIndicator.Text = filterString.ToString();
   }

   private void SetTrackProgress( double value )
   {
      value = Math.Clamp( value, 0f, 1f );
      TrackIndicator.Width = value * PositionTrack.ActualWidth;
   }

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

      TimePreviewText.Text = _timeProvider.GetTimeStringAtPosition( TrackPreview.Width / PositionTrack.ActualWidth );
      Canvas.SetLeft( TimePreview, TrackPreview.Width - ( TimePreview.ActualWidth / 2 ) );
   }

   private async void OnTrackClicked( object sender, RoutedEventArgs e )
   {
      var newPosition = (float)( TrackPreview.Width / PositionTrack.ActualWidth );

      // Snap to the beginning if clicking early enough
      if ( newPosition < 0.01f )
      {
         newPosition = 0;
      }

      if ( _playbackTimer.IsEnabled )
      {
         _player.SetPause( true );
         await Task.Delay( 200 ).ConfigureAwait( true );
      }

      _player.Position = newPosition;
      SetTrackProgress( newPosition );
      CurrentTimeLabel.Text = _timeProvider.GetCurrentTimeString();
   }
}
