using System;
using System.Diagnostics;
using System.Globalization;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcTimeProvider
{
   private readonly VlcManager _vlc;
   private readonly Stopwatch _playbackStopwatch = new();
   private TimeSpan _stopwatchOffset = TimeSpan.Zero;

   public TimeSpan Duration { get; set; }

   public VlcTimeProvider( VlcManager vlc )
   {
      _vlc = vlc;
      _vlc.MediaOpened += OnMediaOpened;
      _vlc.MediaClosing += OnMediaClosing;
   }

   private void OnMediaOpened( object sender, EventArgs e )
   {
      _vlc.Player.Playing += OnPlaying;
      _vlc.Player.Paused += OnPaused;
   }

   private void OnMediaClosing( object sender, EventArgs e )
   {
      _playbackStopwatch.Stop();
      _vlc.Player.Playing -= OnPlaying;
      _vlc.Player.Paused -= OnPaused;
   }

   private void OnPlaying( object sender, EventArgs e )
   {
      _stopwatchOffset = TimeSpan.FromMilliseconds( _vlc.Player.Time );
      _playbackStopwatch.Restart();
   }

   private void OnPaused( object sender, EventArgs e ) => _playbackStopwatch.Stop();

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
         return TimeSpan.FromMilliseconds( _vlc.Player.Time );
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
         return _vlc.Player.Position;
      }
      catch
      {
         return 0;
      }
   }
}