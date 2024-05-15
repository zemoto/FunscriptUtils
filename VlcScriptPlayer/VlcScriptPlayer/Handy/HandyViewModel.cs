using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace VlcScriptPlayer.Handy;

internal sealed partial class HandyViewModel : ObservableObject
{
   [property: JsonIgnore]
   [ObservableProperty]
   private bool _requestInProgress;

   [property: JsonIgnore]
   [ObservableProperty]
   private bool _isConnected;

   [ObservableProperty]
   private string _connectionId;

   [ObservableProperty]
   private int _desiredOffset = -125;

   [ObservableProperty]
   private double _desiredSlideMin;

   [ObservableProperty]
   private double _desiredSlideMax = 100;

   [ObservableProperty]
   private bool _setOptionsWhenSyncing = true;

   [JsonIgnore]
   public ICommand ConnectCommand { get; set; }

   [JsonIgnore]
   public ICommand SetOffsetCommand { get; set; }

   [JsonIgnore]
   public ICommand SetRangeCommand { get; set; }
}
