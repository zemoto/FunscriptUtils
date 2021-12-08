using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace FunscriptUtils.Utils
{
   internal sealed class VideoWrapper : IDisposable
   {
      private readonly VideoCapture _video;

      public VideoWrapper( string videoFilePath ) => _video = new VideoCapture( videoFilePath );

      public Size GetDimensions() => new( (int)_video.Get( CapProp.FrameWidth ), (int)_video.Get( CapProp.FrameHeight ) );

      public IEnumerable<Image<Gray, byte>> GetCroppedFrames( Rectangle rect )
      {
         var frame = _video.QueryFrame();
         while ( frame != null )
         {
            yield return new Mat( frame, rect ).ToImage<Gray, byte>();
            frame = _video.QueryFrame();
         }
      }

      public long GetCurrentFrameTime() => (long)_video.Get( CapProp.PosMsec );

      public TimeSpan GetDuration() => TimeSpan.FromSeconds( _video.Get( CapProp.FrameCount ) / _video.Get( CapProp.Fps ) );

      public void Dispose() => _video?.Dispose();
   }
}