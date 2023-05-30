using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Windows;

namespace VlcScriptPlayer;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _vlc = new();
   private readonly Timer _hideMarqueeTimer;
   public MediaPlayer Player { get; }

   public event EventHandler MediaSetupComplete;

   public VlcManager()
   {
      _hideMarqueeTimer = new Timer( OnHideMarqueeTimerEllapsed, null, Timeout.Infinite, -1 );

      Player = new MediaPlayer( _vlc )
      {
         FileCaching = 3000,
         EnableHardwareDecoding = true
      };

      Player.SetMarqueeInt( VideoMarqueeOption.Position, 6 );
      Player.SetMarqueeInt( VideoMarqueeOption.Opacity, 192 );

      Player.EndReached += OnEndReached;
      Player.VolumeChanged += OnVolumeChanged;
      Player.Paused += OnPlayerPaused;
   }

   public void Dispose()
   {
      _vlc.Dispose();
      Player.Dispose();
      _hideMarqueeTimer.Dispose();
   }

   public void OpenVideo( string filePath )
   {
      Player.Buffering += OnPlayerFirstTimeBuffering;
      var media = new Media( _vlc, new Uri( filePath ) );
      media.Parse();
      Player.Play( media );
   }

   private void OnPlayerFirstTimeBuffering( object sender, MediaPlayerBufferingEventArgs e )
   {
      if ( e.Cache < 100 )
      {
         return;
      }

      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         Player.Buffering -= OnPlayerFirstTimeBuffering;
         Player.SetPause( true );
         Player.Time = 0;

         Thread.Sleep( 400 ); // Give VLC time to process
         Application.Current.Dispatcher.BeginInvoke( () => MediaSetupComplete?.Invoke( this, EventArgs.Empty ) );
      } );
   }

   private void OnHideMarqueeTimerEllapsed( object state ) => Player.SetMarqueeInt( VideoMarqueeOption.Enable, 0 );

   private void OnEndReached( object sender, EventArgs e ) => _ = ThreadPool.QueueUserWorkItem( _ => Player.Stop() );

   private void OnVolumeChanged( object sender, MediaPlayerVolumeChangedEventArgs e )
   {
      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         if ( e.Volume < 0 )
         {
            return;
         }

         var roundedVolume = (int)( Math.Round( e.Volume, 2 ) * 100 );
         Player.SetMarqueeString( VideoMarqueeOption.Text, roundedVolume.ToString() );
         Player.SetMarqueeInt( VideoMarqueeOption.Enable, 1 );
         _hideMarqueeTimer.Change( 1000, -1 );
      } );
   }

   // Force the player to resync itself with where it paused, as this can be incorrect sometimes
   private void OnPlayerPaused( object sender, EventArgs e ) => Player.Position -= 0.0001f;
}
