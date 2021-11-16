using System;
using System.Drawing;
using FunscriptUtils.Utils;

namespace FunscriptUtils.Generating
{
   internal sealed class FapHeroGenerator : IDisposable
   {
      private readonly string _videoFilePath;
      private readonly VideoWrapper _video;
      private readonly FrameMatcher _matcher;
      private readonly int _templateOffset;

      public FapHeroGenerator( GenerationParams generationParams )
      {
         _videoFilePath = generationParams.VideoFilePath;
         _video = new VideoWrapper( generationParams.VideoFilePath );
         _matcher = new FrameMatcher( generationParams.GenerationTemplateFilePath, generationParams.TemplateThreshold );
         _templateOffset = generationParams.TemplateOffset;
      }

      public void GenerateFunscript()
      {
         var actionGenerator = new ActionGenerator();
         var funscript = FunscriptFactory.CreateFresh();

         var templateDimensions = _matcher.GetTemplateDimensions();
         var videoDimensions = _video.GetDimensions();

         var cropRect = new Rectangle(
            ( videoDimensions.Width / 2 ) - ( templateDimensions.Width / 2 ),
            videoDimensions.Height - templateDimensions.Height - _templateOffset,
            templateDimensions.Width,
            templateDimensions.Height );

         foreach ( var croppedFrame in _video.GetCroppedFrames( cropRect ) )
         {
            var frameTime = _video.GetCurrentFrameTime();

            _matcher.MatchFrame( frameTime, croppedFrame );

            croppedFrame.Dispose();
            ConsoleWriter.WriteReport( "Scanning Video", (int)frameTime, true );
         }

         ConsoleWriter.Commit();
         foreach ( var matchTime in _matcher.GetMatchTimes() )
         {
            funscript.Actions.Add( actionGenerator.GetNextAction( matchTime ) );
         }

         ConsoleWriter.WriteReport( "Generated Actions", funscript.Actions.Count );
         ConsoleWriter.Commit();

         funscript.Save( _videoFilePath );
      }

      public void Dispose()
      {
         _video?.Dispose();
         _matcher?.Dispose();
      }
   }
}