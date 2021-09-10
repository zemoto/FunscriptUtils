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
            funscript.Actions.Add( GetNextFunscriptAction( matchTime ) );
         }

         ConsoleWriter.WriteReport( "Generated Actions", funscript.Actions.Count );
         ConsoleWriter.Commit();

         funscript.Save( _videoFilePath );
      }

      private int _nextPosition;

      private FunscriptAction GetNextFunscriptAction( long time )
      {
         var action = new FunscriptAction
         {
            Position = _nextPosition,
            Time = time
         };

         _nextPosition = _nextPosition switch
         {
            0 => 100,
            100 => 0,
            _ => throw new ArgumentException( "How the heck did this happen?" )
         };

         return action;
      }

      public void Dispose()
      {
         _video?.Dispose();
         _matcher?.Dispose();
      }
   }
}