using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace FunscriptUtils.Generating
{
   internal sealed class FrameMatcher : IDisposable
   {
      private const double MatchTolerance = 0.85;

      private readonly Image<Gray, byte> _templateImage;
      private readonly int _templateThreshold;
      private readonly List<(long time, double matchValue)> _frameMatches = new();

      public FrameMatcher( string templateimageFilePath, int templateThreshold )
      {
         _templateImage = new Image<Gray, byte>( templateimageFilePath ).ThresholdBinary( new Gray( _templateThreshold ), new Gray( 255 ) );
         _templateThreshold = templateThreshold;
      }

      public Size GetTemplateDimensions() => new( _templateImage.Width, _templateImage.Height );

      public void MatchFrame( long time, Image<Gray, byte> image )
      {
         using var thresholdImage = image.ThresholdBinary( new Gray( _templateThreshold ), new Gray( 255 ) );
         using var result = thresholdImage.MatchTemplate( _templateImage, TemplateMatchingType.CcorrNormed );

         result.MinMax( out _, out var maxValues, out _, out _ );
         if ( maxValues[0] > MatchTolerance )
         {
            _frameMatches.Add( ( time, maxValues[0] ) );
         }
      }

      public IEnumerable<long> GetMatchTimes()
      {
         var currentBestMatchIdx = -1;
         for ( int i = 0; i < _frameMatches.Count; i++ )
         {
            var (_, matchValue) = _frameMatches[i];
            if ( currentBestMatchIdx != -1 )
            {
               if ( matchValue >= _frameMatches[currentBestMatchIdx].matchValue )
               {
                  currentBestMatchIdx = i;
               }
               else if ( matchValue < _frameMatches[currentBestMatchIdx].matchValue )
               {
                  yield return _frameMatches[currentBestMatchIdx].time;
                  currentBestMatchIdx = -1;
               }
            }
            else if ( i == 0 || matchValue > _frameMatches[i - 1].matchValue )
            {
               currentBestMatchIdx = i;
            }
         }
      }

      public void Dispose() => _templateImage?.Dispose();
   }
}