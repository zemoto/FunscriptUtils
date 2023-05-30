using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal partial class PlayPauseIndicator
{
   public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register( nameof( Player ), typeof( MediaPlayer ), typeof( PlayPauseIndicator ), new PropertyMetadata( null, OnPlayerChanged ) );
   public MediaPlayer Player
   {
      get => (MediaPlayer)GetValue( PlayerProperty );
      set => SetValue( PlayerProperty, value );
   }
   private static void OnPlayerChanged( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( (PlayPauseIndicator)d ).OnPlayerChanged();
   private void OnPlayerChanged()
   {
      Player.Playing += OnMediaPlaying;
      Player.Paused += OnMediaPaused;
   }

   private static readonly ScaleTransform _scaleTransform = new( 0.5, 0.5 );
   private static readonly DoubleAnimation _animation = new( 0.5, 1.0, TimeSpan.FromMilliseconds( 250 ) );

   public PlayPauseIndicator()
   {
      _animation.Completed += OnAnimationCompleted;

      InitializeComponent();

      LayoutTransform = _scaleTransform;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      var player = Player;
      if ( player is null )
      {
         return;
      }

      player.Playing -= OnMediaPlaying;
      player.Paused -= OnMediaPaused;
   }

   private void OnAnimationCompleted( object sender, EventArgs e )
   {
      Visibility = Visibility.Collapsed;
      _scaleTransform.ScaleX = 0.5;
      _scaleTransform.ScaleY = 0.5;
   }

   private void OnMediaPlaying( object sender, EventArgs e )
   {
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
}
