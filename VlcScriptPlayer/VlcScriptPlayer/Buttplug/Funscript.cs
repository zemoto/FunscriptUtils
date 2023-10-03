using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace VlcScriptPlayer.Buttplug;

public sealed class FunscriptAction
{
   private int _position;
   [JsonProperty( PropertyName = "pos" )]
   public int Position
   {
      get => _position;
      set => _position = Math.Clamp( value, 0, 100 );
   }

   [JsonProperty( PropertyName = "at" )]
   public long Time { get; set; }
}

internal sealed class VibrationAction
{
   private double _intensity;
   public double Intensity
   {
      get => _intensity;
      set => _intensity = Math.Clamp( value, 0.0, 1.0 );
   }

   public long Time { get; set; }
}

internal sealed class Funscript
{
   public static Funscript Load( string filePath )
   {
      var funscript = JsonConvert.DeserializeObject<Funscript>( File.ReadAllText( filePath ) );
      funscript.GenerateVibrationActions();
      return funscript;
   }

   private void GenerateVibrationActions()
   {
      const int blockInterval = 166;

      for ( int i = 0; ; i++ )
      {
         var action = OriginalActions[i];
         VibrationActions.Add( new VibrationAction
         {
            Time = action.Time,
            Intensity = PositionToIntensity( action.Position )
         } );

         if ( i == OriginalActions.Count - 1 )
         {
            break;
         }

         var nextAction = OriginalActions[i + 1];
         if ( nextAction.Time - action.Time < blockInterval * 2 )
         {
            continue;
         }

         long currentTime = action.Time + blockInterval;
         while ( currentTime < nextAction.Time - ( blockInterval / 2 ) )
         {
            VibrationActions.Add( new VibrationAction
            {
               Time = currentTime,
               Intensity = GetIntensityAtTime( currentTime )
            } );

            currentTime += blockInterval;
         }
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

   [JsonProperty( PropertyName = "actions" )]
   public List<FunscriptAction> OriginalActions { get; set; }

   [JsonIgnore]
   public List<VibrationAction> VibrationActions { get; } = new();
}