using System.Text.Json.Serialization;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Config;

internal sealed class HandyConfig : ViewModelBase
{
   private bool _isConnected;
   [JsonIgnore]
   public bool IsConnected
   {
      get => _isConnected;
      set => SetProperty( ref _isConnected, value );
   }

   private string _connectionId;
   public string ConnectionId
   {
      get => _connectionId;
      set => SetProperty( ref _connectionId, value );
   }

   private int _desiredOffset = -125;
   public int DesiredOffset
   {
      get => _desiredOffset;
      set => SetProperty( ref _desiredOffset, value );
   }

   private int _currentOffset;
   [JsonIgnore]
   public int CurrentOffset
   {
      get => _currentOffset;
      set => SetProperty( ref _currentOffset, value );
   }

   private double _desiredSlideMin;
   public double DesiredSlideMin
   {
      get => _desiredSlideMin;
      set => SetProperty( ref _desiredSlideMin, value );
   }

   private double _desiredSlideMax = 100;
   public double DesiredSlideMax
   {
      get => _desiredSlideMax;
      set => SetProperty( ref _desiredSlideMax, value );
   }

   private double _currentSlideMin;
   [JsonIgnore]
   public double CurrentSlideMin
   {
      get => _currentSlideMin;
      set => SetProperty( ref _currentSlideMin, value );
   }

   private double _currentSlideMax;
   [JsonIgnore]
   public double CurrentSlideMax
   {
      get => _currentSlideMax;
      set => SetProperty( ref _currentSlideMax, value );
   }
}
