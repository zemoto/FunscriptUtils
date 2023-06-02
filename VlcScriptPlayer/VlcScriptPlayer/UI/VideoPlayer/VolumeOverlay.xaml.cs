using LibVLCSharp.Shared;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal partial class VolumeControl
{
   private readonly DispatcherTimer _fadeOutTimer;
   private static readonly DoubleAnimation _fadeOutAnimation = new( 1.0, 0.0, TimeSpan.FromMilliseconds( 250 ) );
   private MediaPlayer _player;

   public VolumeControl()
   {
      _fadeOutTimer = new DispatcherTimer( TimeSpan.FromSeconds( 1 ), DispatcherPriority.Normal, OnFadeOutTimerTick, Dispatcher ) { IsEnabled = false };
      InitializeComponent();
   }

   public void SetPlayer( MediaPlayer player )
   {
      _player = player;
      _player.VolumeChanged += OnVolumeChanged;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e ) => _player.VolumeChanged -= OnVolumeChanged;

   private void OnFadeOutTimerTick( object sender, EventArgs e )
   {
      _fadeOutTimer.Stop();
      BeginAnimation( OpacityProperty, _fadeOutAnimation );
   }

   private void OnVolumeChanged( object sender, MediaPlayerVolumeChangedEventArgs e )
   {
      Dispatcher.Invoke( () =>
      {
         _fadeOutTimer.Stop();
         VolumeTextBlock.Text = $"Volume {Math.Round( e.Volume * 100.0 )}%";
         VolumeIndicator.Height = e.Volume * VolumeTrack.ActualHeight;

         BeginAnimation( OpacityProperty, null );
         Opacity = 1;
         _fadeOutTimer.Start();
      } );
   }
}
