using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using VlcScriptPlayer.UI;
using VlcScriptPlayer.Vlc.Filter;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _libvlc = new();
   private readonly FilterViewModel _filterSettings;
   private readonly PlaybackSettingsViewModel _playbackSettings;

   private bool _playbackEnabled = true;

   public MarqueeViewModel Marquee { get; } = new();
   public VlcFilter Filter { get; }
   public MediaPlayer Player { get; }
   public VlcTimeProvider TimeProvider { get; }
   public VlcVolumeWrapper VolumeManager { get; }

   public event EventHandler MediaOpened;
   public event EventHandler MediaClosed;

   private DateTime _lastPauseToggleTime = DateTime.MinValue;

   public VlcManager( FilterViewModel filterVm, PlaybackSettingsViewModel playbackVm )
   {
      _filterSettings = filterVm;
      _playbackSettings = playbackVm;
      _playbackSettings.AudioOutputs = _libvlc.AudioOutputs.Skip( 3 ).Select( x => x.Name ).ToList();
      _playbackSettings.ShowAdvancedPlaybackSettingsCommand = new RelayCommand( ShowAdvancedPlaybackSettings );

      Player = new MediaPlayer( _libvlc );
      Filter = new VlcFilter( Player, Marquee );
      TimeProvider = new VlcTimeProvider( Player );
      VolumeManager = new VlcVolumeWrapper( Player, Filter );

      Player.Paused += OnPlayerPaused;
      Player.EndReached += OnPlayerEndReached;

      SetPlaybackSettings();
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
      Player.Playing += OnPlayerInitialPlaying;
      var media = new Media( _libvlc, new Uri( filePath ) );
      _ = Player.Play( media );
   }

   public void CloseVideo()
   {
      Player.Playing -= OnPlayerInitialPlaying;
      Player.Paused -= OnPlayerPausedAfterInitialPlaying;
      Filter.UnsetFilters();
      Player.Stop();
      Player.Media?.Dispose();
      Player.Media = null;
      Marquee.Enabled = false;

      MediaClosed?.Invoke( this, EventArgs.Empty );
   }

   public void TogglePlayPause()
   {
      if ( !_playbackEnabled || DateTime.Now < _lastPauseToggleTime + TimeSpan.FromSeconds( 1 ) )
      {
         return;
      }

      if ( Player.CanPause )
      {
         Player.Pause();
      }
      else
      {
         _ = Player.Play();
      }

      _lastPauseToggleTime = DateTime.Now;
   }

   public void SetPlaybackEnabled( bool enabled )
   {
      if ( !enabled )
      {
         Player.SetPause( true );
      }

      _playbackEnabled = enabled;
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
      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         TimeProvider.Duration = TimeSpan.FromMilliseconds( Player.Media.Duration );
         Marquee.Enabled = true;
         Player.Time = 0;

         MediaOpened?.Invoke( this, EventArgs.Empty );

         if ( _playbackSettings.Autoplay && _playbackEnabled )
         {
            _ = Player.Play();
         }
      } );
   }

   private void ShowAdvancedPlaybackSettings()
   {
      var dialog = new AdvancedPlaybackSettingsWindow( _playbackSettings ) { Owner = Application.Current.MainWindow };
      _ = dialog.ShowDialog();
      SetPlaybackSettings();
   }

   private void SetPlaybackSettings()
   {
      _ = Player.SetAudioOutput( _playbackSettings.SelectedAudioOutput );
      Player.FileCaching = _playbackSettings.CacheSize;
      Player.EnableHardwareDecoding = _playbackSettings.UseHardwareDecoding;
   }

   // Force the player to resync itself with where it paused, as this can be incorrect sometimes
   private void OnPlayerPaused( object sender, EventArgs e ) => Player.Position -= float.Epsilon;

   private void OnPlayerEndReached( object sender, EventArgs e )
   {
      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         if ( _playbackSettings.Loop )
         {
            _ = Player.Play( Player.Media );
         }
         else
         {
            CloseVideo();
         }
      } );
   }
}
