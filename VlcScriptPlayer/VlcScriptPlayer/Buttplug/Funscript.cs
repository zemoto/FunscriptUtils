using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace VlcScriptPlayer.Buttplug;

public sealed class FunscriptAction
{
   [JsonProperty( PropertyName = "pos" )]
   public int Position { get; init; }

   [JsonProperty( PropertyName = "at" )]
   public long Time { get; init; }
}

internal sealed class VibrationAction
{
   private double _intensity;
   public double Intensity
   {
      get => _intensity;
      init => _intensity = Math.Round( Math.Clamp( value, 0.0, 1.0 ), 2 );
   }

   public long Time { get; init; }
}

internal sealed class Funscript
{
   private const int _actionsPerSecond = 6;
   private const int _blockInterval = 1000 / _actionsPerSecond;
   private const int _longHoldThreshold = 2000;

   public static Funscript Load( string filePath, int offsetMs, double intensityScale )
   {
      var funscript = JsonConvert.DeserializeObject<Funscript>( File.ReadAllText( filePath ) );
      funscript.GenerateVibrationActions( offsetMs, intensityScale );
      return funscript;
   }

   private void GenerateVibrationActions( int offsetMs, double intensityScale )
   {
      for ( int i = 0; ; i++ )
      {
         var action = OriginalActions[i];
         AddVibrationAction( action.Time + offsetMs, PositionToIntensity( action.Position ) * intensityScale );

         if ( i == OriginalActions.Count - 1 )
         {
            break;
         }

         var nextAction = OriginalActions[i + 1];
         var gap = nextAction.Time - action.Time;
         if ( gap < _blockInterval * 2 )
         {
            continue;
         }

         if ( gap > _longHoldThreshold )
         {
            AddTaperOffVibrations( offsetMs );
            continue;
         }

         long currentTime = action.Time + _blockInterval;
         while ( currentTime < nextAction.Time - ( _blockInterval / 2 ) )
         {
            AddVibrationAction( currentTime + offsetMs, GetIntensityAtTime( currentTime ) * intensityScale );
            currentTime += _blockInterval;
         }
      }
   }

   private void AddTaperOffVibrations( int offsetMs )
   {
      var lastAction = VibrationActions.Last();
      var intensityStep = lastAction.Intensity / _actionsPerSecond;
      var currentIntensity = lastAction.Intensity - intensityStep;
      var currentTime = lastAction.Time + _blockInterval;
      while ( currentTime < lastAction.Time + 1000 )
      {
         AddVibrationAction( currentTime + offsetMs, currentIntensity );
         currentTime += _blockInterval;
         currentIntensity -= intensityStep;
      }
   }

   private double GetIntensityAtTime( long time )
   {
      var firstAction = OriginalActions.Find( x => x.Time <= time ) ?? new FunscriptAction { Time = time, Position = 0 };
      var nextAction = OriginalActions.Find( x => x.Time > time );
      if ( nextAction is null )
      {
         return 0.0;
      }

      var slope = ( nextAction.Position - firstAction.Position ) / (double)( nextAction.Time - firstAction.Time );
      var intercept = firstAction.Position - ( slope * firstAction.Time );
      var positionAtTime = (int)( ( slope * time ) + intercept );
      return PositionToIntensity( positionAtTime );
   }

   private static double PositionToIntensity( int position ) => 1.0 - ( position / 100.0 );

   private void AddVibrationAction( long time, double intensity ) => VibrationActions.Add( new VibrationAction { Time = time, Intensity = intensity } );

   [JsonProperty( PropertyName = "actions" )]
   public List<FunscriptAction> OriginalActions { get; init; }

   public List<VibrationAction> VibrationActions { get; } = new();
}