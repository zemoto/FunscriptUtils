using Newtonsoft.Json;
using System;

namespace FunscriptUtils
{
   public enum ActionRelativePosition
   {
      Top = 0,
      Middle = 1,
      Hold = 2,
      Bottom = 3
   }

   public sealed class FunscriptAction
   {
      public FunscriptAction() { }

      public FunscriptAction( FunscriptAction other )
      {
         Position = other.Position;
         Time = other.Time;
         DesiredGap = other.DesiredGap;
         DesiredGapMaster = other.DesiredGapMaster;
         LastActionBeforeBreak = other.LastActionBeforeBreak;
         RelativePosition = other.RelativePosition;
      }

      private int _position;
      [JsonProperty( PropertyName = "pos" )]
      public int Position
      {
         get => _position;
         set => _position = Math.Clamp( value, 0, 100 );
      }

      [JsonProperty( PropertyName = "at" )]
      public long Time { get; set; }

      private long _desiredGap;
      [JsonIgnore]
      public long DesiredGap
      {
         get => LastActionBeforeBreak ? -1 : DesiredGapMaster?.DesiredGap ?? _desiredGap;
         set => _desiredGap = value;
      }

      [JsonIgnore]
      public FunscriptAction DesiredGapMaster { get; set; }

      [JsonIgnore]
      public bool LastActionBeforeBreak { get; set; }

      [JsonIgnore]
      public ActionRelativePosition RelativePosition { get; set; }

      public double GetSpeedToAction( FunscriptAction next )
      {
         var gapInSeconds = ( next.Time - Time ) / 1000.0;
         var change = Math.Abs( next.Position - Position );

         return change / gapInSeconds;
      }
   }
}
