using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class VolumeControl
{
   private readonly DispatcherTimer _fadeOutTimer;
   private static readonly DoubleAnimation _fadeOutAnimation = new( 1.0, 0.0, TimeSpan.FromMilliseconds( 250 ) );
   private VlcManager _vlc;

   public VolumeControl()
   {
      _fadeOutTimer = new DispatcherTimer( TimeSpan.FromSeconds( 1 ), DispatcherPriority.Normal, OnFadeOutTimerTick, Dispatcher ) { IsEnabled = false };
      InitializeComponent();
   }

   public void SetVlc( VlcManager vlc )
   {
      _vlc = vlc;
      _vlc.VolumeManager.VolumeChanged += OnVolumeChanged;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      if ( _vlc is not null )
      {
         _vlc.VolumeManager.VolumeChanged -= OnVolumeChanged;
      }
   }

   private void OnFadeOutTimerTick( object sender, EventArgs e )
   {
      _fadeOutTimer.Stop();
      BeginAnimation( OpacityProperty, _fadeOutAnimation );
   }

   private void OnVolumeChanged( object sender, EventArgs e )
   {
      Dispatcher.BeginInvoke( () =>
      {
         _fadeOutTimer.Stop();

         if ( _vlc.Filter.VolumeAmpEnabled )
         {
            VolumeTextBlock.Text = "Volume Amped";
            VolumeIndicator.Height = VolumeTrack.ActualHeight;
         }
         else
         {
            var volume = _vlc.VolumeManager.Volume;
            VolumeTextBlock.Text = $"Volume {volume}%";
            VolumeIndicator.Height = ( volume / 100.0 ) * VolumeTrack.ActualHeight;
         }

         BeginAnimation( OpacityProperty, null );
         Opacity = 1;
         _fadeOutTimer.Start();
      } );
   }
}
