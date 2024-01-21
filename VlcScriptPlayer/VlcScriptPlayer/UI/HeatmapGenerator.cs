using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ZemotoCommon;

namespace VlcScriptPlayer.UI;

internal static class HeatmapGenerator
{
   private static readonly List<Color> _speedColors =
   [
      Colors.Transparent,
      Colors.DodgerBlue,
      Colors.Cyan,
      Colors.Lime,
      Colors.Yellow,
      Colors.Red,
   ];

   private static readonly int _maxIndex = _speedColors.Count - 1;

   public static LinearGradientBrush GetHeatmapBrush( Funscript script, long videoDurationMs )
   {
      if ( script.Actions is null || script.Actions.Count < 2 )
      {
         return null;
      }

      const int numSamples = 2048;

      var samples = new (double speedBuffer, int sampleCount)[numSamples];
      var sampleDuration = (double)videoDurationMs / numSamples;
      var numRelevantActions = script.Actions.Count( x => x.Time <= videoDurationMs );

      for ( int i = 0; i < numRelevantActions - 1; i++ )
      {
         var current = script.Actions[i];
         var next = script.Actions[i + 1];

         var currentSampleIdx = (int)( current.Time / sampleDuration );
         var nextSampleIdx = (int)( next.Time / sampleDuration );
         var speed = (double)Math.Abs( next.Position - current.Position ) / ( next.Time - current.Time ) * 1000;

         int j = currentSampleIdx;
         do
         {
            samples[j].speedBuffer += speed;
            samples[j].sampleCount++;
            j++;
         } while ( j < nextSampleIdx );
      }

      var gradientStops = samples.Select( ( s, i ) => new GradientStop( GetColorForSpeed( s.sampleCount > 0 ? s.speedBuffer / s.sampleCount : 0 ), (double)i / ( numSamples - 1 ) ) ).ToList();

      // Remove redundant gradient stops
      for ( int i = numSamples - 2; i >= 1; i-- )
      {
         var prev = gradientStops[i - 1];
         var current = gradientStops[i];
         var next = gradientStops[i + 1];

         if ( prev.Color == current.Color && current.Color == next.Color )
         {
            gradientStops.RemoveAt( i );
         }
      }

      return new LinearGradientBrush( new GradientStopCollection( gradientStops ) );
   }

   private static Color GetColorForSpeed( double speed )
   {
      var colorIndex = Math.Min( speed.MapNumberToRange( 0, 400, 0, _maxIndex ), _maxIndex );
      var lowerIndex = (int)colorIndex;
      if ( colorIndex == lowerIndex )
      {
         return _speedColors[lowerIndex];
      }

      var upperIndex = lowerIndex + 1;
      var upperIntensity = colorIndex - lowerIndex;
      var lowerIntensity = 1 - upperIntensity;

      return Color.FromRgb(
         (byte)( ( _speedColors[lowerIndex].R * lowerIntensity ) + ( _speedColors[upperIndex].R * upperIntensity ) ),
         (byte)( ( _speedColors[lowerIndex].G * lowerIntensity ) + ( _speedColors[upperIndex].G * upperIntensity ) ),
         (byte)( ( _speedColors[lowerIndex].B * lowerIntensity ) + ( _speedColors[upperIndex].B * upperIntensity ) ) );
   }
}
