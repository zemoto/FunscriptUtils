using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class Marquee
{
   private static readonly DoubleAnimationUsingKeyFrames _fadeOutAnimation = new()
   {
      Duration = TimeSpan.FromMilliseconds( 750 ),
      KeyFrames =
      [
         new DiscreteDoubleKeyFrame( 1.0, TimeSpan.Zero ),
         new DiscreteDoubleKeyFrame( 1.0, TimeSpan.FromMilliseconds( 500 ) ),
         new LinearDoubleKeyFrame( 0.0 )
      ]
   };

   private VlcFilter _filter;
   private MarqueeViewModel _marquee;
   private VlcVolumeWrapper _volumeManager;

   public Marquee() => InitializeComponent();

   public void Init( VlcManager vlc )
   {
      _filter = vlc.Filter;
      _marquee = vlc.Marquee;
      _marquee.PropertyChanged += OnMarqueePropertyChanged;

      _volumeManager = vlc.VolumeManager;
      _volumeManager.VolumeChanged += OnVolumeChanged;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      if ( _marquee is not null )
      {
         _marquee.PropertyChanged -= OnMarqueePropertyChanged;
      }

      if ( _volumeManager is not null )
      {
         _volumeManager.VolumeChanged -= OnVolumeChanged;
      }
   }

   private void OnMarqueePropertyChanged( object sender, PropertyChangedEventArgs e )
   {
      if ( e.PropertyName == nameof( _marquee.Text ) )
      {
         _ = Dispatcher.BeginInvoke( DisplayMarquee );
      }
   }

   private void OnVolumeChanged( object sender, EventArgs e ) => _ = Dispatcher.BeginInvoke( () =>
   {
      if ( _filter.VolumeAmpEnabled )
      {
         VolumeIndicatorTransform.ScaleY = 1.0;
         _marquee.SetText( "Volume Amped", MarqueeType.Volume );
      }
      else
      {
         var volume = _volumeManager.Volume;
         VolumeIndicatorTransform.ScaleY = volume / 100.0;
         _marquee.SetText( $"Volume {volume}%", MarqueeType.Volume );
      }
   } );

   private void DisplayMarquee()
   {
      switch ( _marquee.Position )
      {
         case MarqueePosition.Info:
            MarqueeTextBlock.VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Left;
            VolumeTrack.Visibility = Visibility.Collapsed;
            break;
         case MarqueePosition.Volume:
            MarqueeTextBlock.VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Right;
            VolumeTrack.Visibility = Visibility.Visible;
            break;
         case MarqueePosition.Priority:
            MarqueeTextBlock.VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            VolumeTrack.Visibility = Visibility.Collapsed;
            break;
      }

      MarqueeTextBlock.Text = _marquee.Text;

      BeginAnimation( OpacityProperty, null );
      if ( string.IsNullOrEmpty( _marquee.Text ) )
      {
         Opacity = 0;
      }
      else if ( _marquee.IsPerpetual )
      {
         Opacity = 1;
      }
      else
      {
         BeginAnimation( OpacityProperty, _fadeOutAnimation );
      }
   }
}
