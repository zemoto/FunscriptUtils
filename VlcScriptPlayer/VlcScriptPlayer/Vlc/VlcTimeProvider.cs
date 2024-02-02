using LibVLCSharp.Shared;
using System;
using System.Globalization;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcTimeProvider( MediaPlayer player )
{
   public TimeSpan Duration { get; set; }

   public double GetCurrentPlaybackPosition()
   {
      try
      {
         return player.Position;
      }
      catch
      {
         return 0;
      }
   }

   public TimeSpan GetCurrentTime()
   {
      try
      {
         return TimeSpan.FromMilliseconds( player.Time );
      }
      catch
      {
         return TimeSpan.Zero;
      }
   }

   public string GetCurrentTimeString() => TimeSpanToString( GetCurrentTime() );

   public string GetDurationString() => TimeSpanToString( Duration );

   public string GetTimeStringAtPosition( double position ) => TimeSpanToString( position * Duration );

   private string TimeSpanToString( TimeSpan time ) => time.ToString( Duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture );
}