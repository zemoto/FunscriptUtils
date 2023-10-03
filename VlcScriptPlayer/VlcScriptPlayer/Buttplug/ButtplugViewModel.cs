using System.Windows.Input;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ButtplugViewModel : ViewModelBase
{
   private bool _isConnectedToServer;
   public bool IsConnectedToServer
   {
      get => _isConnectedToServer;
      set => SetProperty( ref _isConnectedToServer, value );
   }

   private bool _isConnectedToDevice;
   public bool IsConnectedToDevice
   {
      get => _isConnectedToDevice;
      set => SetProperty( ref _isConnectedToDevice, value );
   }

   private string _deviceName;
   public string DeviceName
   {
      get => _deviceName;
      set => SetProperty( ref _deviceName, value );
   }

   public ICommand ConnectToServerCommand { get; set; }
}