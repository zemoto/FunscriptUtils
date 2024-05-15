using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace VlcScriptPlayer.Buttplug;

internal sealed partial class ButtplugViewModel : ObservableObject
{
   [property: JsonIgnore]
   [ObservableProperty]
   private bool _isConnectedToServer;

   [JsonIgnore]
   public bool IsConnectedToDevice => !string.IsNullOrEmpty( _deviceName );

   [property: JsonIgnore]
   [ObservableProperty]
   [NotifyPropertyChangedFor( nameof( IsConnectedToDevice ) )]
   private string _deviceName;

   [ObservableProperty]
   private int _intensity = 100;

   [ObservableProperty]
   private int _offset = -125;

   [JsonIgnore]
   public ICommand ConnectToServerCommand { get; set; }
}