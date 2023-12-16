using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Windows;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _libvlc = new();
   private readonly VlcMarquee _marquee;
   public VlcFilter Filter { get; }
   public MediaPlayer Player { get; }
   public VlcTimeProvider TimeProvider { get; }
   public VlcVolumeWrapper VolumeManager { get; }

   public event EventHandler MediaOpened;
   public event EventHandler MediaClosed;

   private DateTime _lastPauseToggleTime = DateTime.MinValue;

   public VlcManager()
   {
      Player = new MediaPlayer( _libvlc )
      {
         FileCaching = 3000,
         EnableHardwareDecoding = true
      };

      // Weird hack that ensures first play is in a good initial state.
      // For some reason without this the audio on first play is super quiet.
      Player.Stop();

      _marquee = new VlcMarquee( Player );
      Filter = new VlcFilter( Player, _marquee );
      TimeProvider = new VlcTimeProvider( Player );
      VolumeManager = new VlcVolumeWrapper( Player, Filter );

      Player.Paused += OnPlayerPaused;
   }

   public void Dispose()
   {
      _libvlc.Dispose();
      Filter.Dispose();
      Player.Dispose();
   }

   public void OpenVideo( string filePath, FilterViewModel filterConfig )
   {
      Filter.SetFilters( filterConfig );

      Player.Buffering += OnPlayerFirstTimeBuffering;
      var media = new Media( _libvlc, new Uri( filePath ) );
      Player.Play( media );
   }

   public void CloseVideo()
   {
      Player.Buffering -= OnPlayerFirstTimeBuffering;
      Filter.UnsetFilters();
      Player.Stop();
      Player.Media?.Dispose();
      Player.Media = null;
      _marquee.SetEnabled( false );

      Application.Current.Dispatcher.BeginInvoke( () => MediaClosed?.Invoke( this, EventArgs.Empty ) );
   }

   public void TogglePlayPause()
   {
      if ( DateTime.Now < _lastPauseToggleTime + TimeSpan.FromSeconds( 1 ) )
      {
         return;
      }

      if ( Player.CanPause )
      {
         Player.Pause();
      }
      else
      {
         Player.Play();
      }

      _lastPauseToggleTime = DateTime.Now;
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
         TimeProvider.Duration = TimeSpan.FromMilliseconds( Player.Media.Duration );
         VolumeManager.Volume = 100;

         Thread.Sleep( 500 ); // Give VLC time to process
         Application.Current.Dispatcher.BeginInvoke( () => MediaOpened?.Invoke( this, EventArgs.Empty ) );
         _marquee.SetEnabled( true );
      } );
   }

   // Force the player to resync itself with where it paused, as this can be incorrect sometimes
   private void OnPlayerPaused( object sender, EventArgs e ) => Player.Position -= float.Epsilon;
}
