using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class PlayPauseIndicator
{
   private static readonly ScaleTransform _scaleTransform = new( 0.5, 0.5 );
   private static readonly DoubleAnimation _animation = new( 0.5, 1.0, TimeSpan.FromMilliseconds( 250 ) );
   private MediaPlayer _player;
   private bool _ignoreNextPlayingEvent;

   public PlayPauseIndicator()
   {
      _animation.Completed += OnAnimationCompleted;

      InitializeComponent();

      LayoutTransform = _scaleTransform;
   }

   public void Init( MediaPlayer player )
   {
      _player = player;
      _player.Playing += OnMediaPlaying;
      _player.Paused += OnMediaPaused;
      _player.EndReached += OnPlayerEndReached;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      _animation.Completed -= OnAnimationCompleted;

      if ( _player is not null )
      {
         _player.Playing -= OnMediaPlaying;
         _player.Paused -= OnMediaPaused;
         _player.EndReached -= OnPlayerEndReached;
      }
   }

   private void OnAnimationCompleted( object sender, EventArgs e )
   {
      Visibility = Visibility.Collapsed;
      _scaleTransform.ScaleX = 0.5;
      _scaleTransform.ScaleY = 0.5;
   }

   private void OnMediaPlaying( object sender, EventArgs e )
   {
      if ( _ignoreNextPlayingEvent )
      {
         _ignoreNextPlayingEvent = false;
         return;
      }

      Dispatcher.BeginInvoke( () =>
      {
         Visibility = Visibility.Visible;
         PlayGlyph.Visibility = Visibility.Visible;
         PauseGlyph.Visibility = Visibility.Collapsed;

         _scaleTransform.BeginAnimation( ScaleTransform.ScaleXProperty, _animation );
         _scaleTransform.BeginAnimation( ScaleTransform.ScaleYProperty, _animation );
      } );
   }

   private void OnMediaPaused( object sender, EventArgs e )
   {
      Dispatcher.BeginInvoke( () =>
      {
         Visibility = Visibility.Visible;
         PlayGlyph.Visibility = Visibility.Collapsed;
         PauseGlyph.Visibility = Visibility.Visible;

         _scaleTransform.BeginAnimation( ScaleTransform.ScaleXProperty, _animation );
         _scaleTransform.BeginAnimation( ScaleTransform.ScaleYProperty, _animation );
      } );
   }

   private void OnPlayerEndReached( object sender, EventArgs e ) => _ignoreNextPlayingEvent = true; // If we get another Playing event it's because the media looped
}
