using System;
using System.Collections.Generic;
using System.Linq;

namespace VlcScriptPlayer.Buttplug;

internal sealed class VibrationAction
{
   public long Time { get; }
   public double Intensity { get; }

   public VibrationAction( long time, double intensity )
   {
      Time = time;
      Intensity = Math.Round( Math.Clamp( intensity, 0.0, 1.0 ), 2 );
   }
}

internal sealed class ScriptVibrationConverter
{
   private const int _actionsPerSecond = 6;
   private const int _blockInterval = 1000 / _actionsPerSecond;
   private const int _longHoldThreshold = 2000;

   private readonly List<FunscriptAction> _originalActions;
   private readonly int _offsetMs;
   private readonly double _intensityScale;

   public ScriptVibrationConverter( Funscript script, int offsetMs, double intensityScale )
   {
      _originalActions = script.Actions;
      _offsetMs = offsetMs;
      _intensityScale = intensityScale;
   }

   private List<VibrationAction> _vibrationActions;
   public List<VibrationAction> VibrationActions
   {
      get
      {
         if ( _vibrationActions is null )
         {
            GenerateVibrationActions();
         }

         return _vibrationActions;
      }
   }

   private void GenerateVibrationActions()
   {
      _vibrationActions = new List<VibrationAction>();
      for ( int i = 0; ; i++ )
      {
         var action = _originalActions[i];
         _vibrationActions.Add( new VibrationAction( action.Time + _offsetMs, PositionToIntensity( action.Position ) * _intensityScale ) );
         if ( i == _originalActions.Count - 1 )
         {
            break;
         }

         var nextAction = _originalActions[i + 1];
         var gap = nextAction.Time - action.Time;
         if ( gap > _longHoldThreshold )
         {
            GenerateTaperOffActions();
         }
         else if ( gap >= _blockInterval * 2 )
         {
            GenerateIntermediateActions( action, nextAction );
         }
      }
   }

   private void GenerateTaperOffActions()
   {
      var lastAction = _vibrationActions.Last();
      var intensityStep = lastAction.Intensity / _actionsPerSecond;
      var currentIntensity = lastAction.Intensity - intensityStep;
      long currentTime = lastAction.Time + _blockInterval;
      while ( currentTime < lastAction.Time + 1000 )
      {
         _vibrationActions.Add( new VibrationAction( currentTime + _offsetMs, currentIntensity ) );
         currentTime += _blockInterval;
         currentIntensity -= intensityStep;
      }
   }

   private void GenerateIntermediateActions( FunscriptAction firstAction, FunscriptAction nextAction )
   {
      var slope = ( nextAction.Position - firstAction.Position ) / (double)( nextAction.Time - firstAction.Time );
      var intercept = firstAction.Position - ( slope * firstAction.Time );

      long currentTime = firstAction.Time + _blockInterval;
      while ( currentTime < nextAction.Time - ( _blockInterval / 2 ) )
      {
         var positionAtTime = (int)( ( slope * currentTime ) + intercept );
         _vibrationActions.Add( new VibrationAction( currentTime + _offsetMs, PositionToIntensity( positionAtTime ) * _intensityScale ) );
         currentTime += _blockInterval;
      }
   }

   private static double PositionToIntensity( int position ) => 1.0 - ( position / 100.0 );
}
