using System.Text.Json.Serialization;
using System.Windows.Input;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ButtplugViewModel : ViewModelBase
{
   private bool _isConnectedToServer;
   [JsonIgnore]
   public bool IsConnectedToServer
   {
      get => _isConnectedToServer;
      set => SetProperty( ref _isConnectedToServer, value );
   }

   [JsonIgnore]
   public bool IsConnectedToDevice => !string.IsNullOrEmpty( DeviceName );

   private string _deviceName;
   [JsonIgnore]
   public string DeviceName
   {
      get => _deviceName;
      set
      {
         if ( SetProperty( ref _deviceName, value ) )
         {
            OnPropertyChanged( nameof( IsConnectedToDevice ) );
         }
      }
   }

   private int _intensity = 100;
   public int Intensity
   {
      get => _intensity;
      set => SetProperty( ref _intensity, value );
   }

   private int _offset = -125;
   public int Offset
   {
      get => _offset;
      set => SetProperty( ref _offset, value );
   }

   [JsonIgnore]
   public ICommand ConnectToServerCommand { get; set; }
}