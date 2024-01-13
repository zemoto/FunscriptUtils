using System;
using System.Windows;
using System.Windows.Media.Animation;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal sealed partial class VolumeControl
{
   private static readonly DoubleAnimationUsingKeyFrames _fadeOutAnimation = new()
   {
      Duration = TimeSpan.FromMilliseconds( 1250 ),
      KeyFrames =
      [
         new DiscreteDoubleKeyFrame( 1.0, TimeSpan.Zero ),
         new DiscreteDoubleKeyFrame( 1.0, TimeSpan.FromSeconds( 1 ) ),
         new LinearDoubleKeyFrame( 0.0 )
      ]
   };

   private VlcFilter _filter;
   private VlcVolumeWrapper _volumeManager;

   public VolumeControl() => InitializeComponent();

   public void Init( VlcManager vlc )
   {
      _filter = vlc.Filter;
      _volumeManager = vlc.VolumeManager;
      _volumeManager.VolumeChanged += OnVolumeChanged;
   }

   private void OnUnloaded( object sender, RoutedEventArgs e )
   {
      if ( _volumeManager is not null )
      {
         _volumeManager.VolumeChanged -= OnVolumeChanged;
      }
   }

   private void OnVolumeChanged( object sender, EventArgs e )
   {
      Dispatcher.BeginInvoke( () =>
      {
         if ( _filter.VolumeAmpEnabled )
         {
            VolumeTextBlock.Text = "Volume Amped";
            VolumeIndicatorTransform.ScaleY = 1.0;
         }
         else
         {
            var volume = _volumeManager.Volume;
            VolumeTextBlock.Text = $"Volume {volume}%";
            VolumeIndicatorTransform.ScaleY = volume / 100.0;
         }

         BeginAnimation( OpacityProperty, null );
         BeginAnimation( OpacityProperty, _fadeOutAnimation );
      } );
   }
}
