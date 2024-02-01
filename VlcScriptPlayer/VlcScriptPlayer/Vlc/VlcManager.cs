using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Windows;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _libvlc = new();
   private readonly FilterViewModel _filterSettings;
   private readonly PlaybackViewModel _playbackSettings;

   public VlcMarquee Marquee { get; }
   public VlcFilter Filter { get; }
   public MediaPlayer Player { get; }
   public VlcTimeProvider TimeProvider { get; }
   public VlcVolumeWrapper VolumeManager { get; }

   public event EventHandler MediaOpened;
   public event EventHandler MediaClosing;

   private DateTime _lastPauseToggleTime = DateTime.MinValue;

   public VlcManager( FilterViewModel filterVm, PlaybackViewModel playbackVm )
   {
      _filterSettings = filterVm;
      _playbackSettings = playbackVm;

      Player = new MediaPlayer( _libvlc )
      {
         FileCaching = 3000,
         EnableHardwareDecoding = true
      };

      // Weird hack that ensures first play is in a good initial state.
      // For some reason without this the audio on first play is super quiet.
      Player.Stop();

      Marquee = new VlcMarquee( Player );
      Filter = new VlcFilter( Player, Marquee );
      TimeProvider = new VlcTimeProvider( this );
      VolumeManager = new VlcVolumeWrapper( Player, Filter );

      Player.Paused += OnPlayerPaused;
      Player.EndReached += OnPlayerEndReached;
   }

   public void Dispose()
   {
      _libvlc.Dispose();
      Filter.Dispose();
      Player.Dispose();
   }

   public void OpenVideo( string filePath )
   {
      Filter.SetFilters( _filterSettings );
      Player.Buffering += OnPlayerFirstTimeBuffering;
      var media = new Media( _libvlc, new Uri( filePath ) );
      Player.Play( media );
   }

   public void CloseVideo()
   {
      Application.Current.Dispatcher.Invoke( () => MediaClosing?.Invoke( this, EventArgs.Empty ) );

      Player.Buffering -= OnPlayerFirstTimeBuffering;
      Filter.UnsetFilters();
      Player.Stop();
      Player.Media?.Dispose();
      Player.Media = null;
      Marquee.SetEnabled( false );
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
         Marquee.SetEnabled( true );
      } );
   }

   // Force the player to resync itself with where it paused, as this can be incorrect sometimes
   private void OnPlayerPaused( object sender, EventArgs e ) => Player.Position -= float.Epsilon;

   private void OnPlayerEndReached( object sender, EventArgs e )
   {
      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         if ( _playbackSettings.Loop )
         {
            Player.Play( Player.Media );
         }
         else
         {
            CloseVideo();
         }
      } );
   }
}
