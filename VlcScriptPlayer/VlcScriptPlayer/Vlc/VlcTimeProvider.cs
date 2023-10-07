using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Globalization;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcTimeProvider
{
   private readonly MediaPlayer _player;
   private readonly Stopwatch _playbackStopwatch = new();
   private TimeSpan _stopwatchOffset = TimeSpan.Zero;

   public TimeSpan Duration { get; set; }

   public VlcTimeProvider( MediaPlayer player )
   {
      _player = player;
      _player.Playing += OnPlaying;
      _player.Paused += OnPausedOrStopped;
      _player.Stopped += OnPausedOrStopped;
   }

   private void OnPlaying( object sender, EventArgs e )
   {
      _stopwatchOffset = TimeSpan.FromMilliseconds( _player.Time );
      _playbackStopwatch.Restart();
   }

   private void OnPausedOrStopped( object sender, EventArgs e ) => _playbackStopwatch.Stop();

   public TimeSpan GetCurrentPlaybackTime() => _playbackStopwatch.IsRunning ? _playbackStopwatch.Elapsed + _stopwatchOffset : GetPlayerTime();

   public double GetCurrentPlaybackPosition() => _playbackStopwatch.IsRunning ? ( _playbackStopwatch.Elapsed + _stopwatchOffset ) / Duration : GetPlayerPlaybackPosition();

   public string GetCurrentTimeString() => TimeSpanToString( GetCurrentPlaybackTime() );

   public string GetDurationString() => TimeSpanToString( Duration );

   public string GetTimeStringAtPosition( double position ) => TimeSpanToString( position * Duration );

   private string TimeSpanToString( TimeSpan time ) => time.ToString( Duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture );

   private TimeSpan GetPlayerTime()
   {
      try
      {
         return TimeSpan.FromMilliseconds( _player.Time );
      }
      catch
      {
         return TimeSpan.Zero;
      }
   }

   private double GetPlayerPlaybackPosition()
   {
      try
      {
         return _player.Position;
      }
      catch
      {
         return 0;
      }
   }
}