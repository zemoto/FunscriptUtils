using System.Text.Json.Serialization;
using System.Windows.Input;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyViewModel : ViewModelBase
{
   private bool _requestInProgress;
   [JsonIgnore]
   public bool RequestInProgress
   {
      get => _requestInProgress;
      set => SetProperty( ref _requestInProgress, value );
   }

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

   private bool _setOptionsWhenSyncing = true;
   public bool SetOptionsWhenSyncing
   {
      get => _setOptionsWhenSyncing;
      set => SetProperty( ref _setOptionsWhenSyncing, value );
   }

   [JsonIgnore]
   public ICommand ConnectCommand { get; set; }

   [JsonIgnore]
   public ICommand SetOffsetCommand { get; set; }

   [JsonIgnore]
   public ICommand SetRangeCommand { get; set; }
}
