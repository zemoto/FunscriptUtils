using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _libvlc = new();
   private readonly FilterViewModel _filterSettings;
   private readonly PlaybackViewModel _playbackSettings;

   public MarqueeViewModel Marquee { get; } = new();
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
      _playbackSettings.AudioOutputs = _libvlc.AudioOutputs.Skip( 3 ).Select( x => x.Name ).ToList();

      Player = new MediaPlayer( _libvlc )
      {
         FileCaching = 3000,
         EnableHardwareDecoding = true
      };

      Filter = new VlcFilter( Player, Marquee );
      TimeProvider = new VlcTimeProvider( Player );
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
      Player.SetAudioOutput( _playbackSettings.SelectedAudioOutput );
      Player.Playing += OnPlayerInitialPlaying;
      var media = new Media( _libvlc, new Uri( filePath ) );
      Player.Play( media );
   }

   public void CloseVideo()
   {
      Application.Current.Dispatcher.Invoke( () => MediaClosing?.Invoke( this, EventArgs.Empty ) );

      Player.Playing -= OnPlayerInitialPlaying;
      Player.Paused -= OnPlayerPausedAfterInitialPlaying;
      Filter.UnsetFilters();
      Player.Stop();
      Player.Media?.Dispose();
      Player.Media = null;
      Marquee.Enabled = false;
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

   private void OnPlayerInitialPlaying( object sender, EventArgs e )
   {
      Player.Playing -= OnPlayerInitialPlaying;
      Player.Paused += OnPlayerPausedAfterInitialPlaying;
      Player.SetPause( true );
   }

   private void OnPlayerPausedAfterInitialPlaying( object sender, EventArgs e )
   {
      Player.Paused -= OnPlayerPausedAfterInitialPlaying;
      Application.Current.Dispatcher.BeginInvoke( () =>
      {
         TimeProvider.Duration = TimeSpan.FromMilliseconds( Player.Media.Duration );
         VolumeManager.Volume = 100;
         Marquee.Enabled = true;
         Player.Time = 0;

         MediaOpened?.Invoke( this, EventArgs.Empty );

         if ( _playbackSettings.Autoplay )
         {
            Player.Play();
         }
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
