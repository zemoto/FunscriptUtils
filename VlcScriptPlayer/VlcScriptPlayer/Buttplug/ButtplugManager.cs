using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using System;
using System.Threading.Tasks;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ButtplugManager : ISyncTarget, IDisposable
{
   private readonly ButtplugViewModel _model;
   private readonly ButtplugClient _client;

   private ButtplugClientDevice _device;
   private ScriptPlayer _scriptPlayer;

   private bool _scanning;

   public ButtplugManager( ButtplugViewModel model )
   {
      _model = model;
      _model.ConnectToServerCommand = new RelayCommand( () => _ = ConnectToServerAsync() );

      _client = new ButtplugClient( "VlcScriptPlayer" );
      _client.DeviceAdded += OnDeviceAdded;
      _client.DeviceRemoved += OnDeviceRemoved;
      _client.ServerDisconnect += OnServerDisconnect;
   }

   public void Dispose() => _ = Task.Run( _client.Dispose );

   private async Task ConnectToServerAsync()
   {
      _model.IsConnectedToServer = false;
      _model.DeviceName = string.Empty;

      const string serverUri = "ws://localhost:12345";
      var connector = new ButtplugWebsocketConnector( new Uri( serverUri ) );
      Logger.LogRequest( "Connecting to Intiface Server" );
      try
      {
         await _client.ConnectAsync( connector );
         Logger.LogRequestSuccess();
         _model.IsConnectedToServer = true;

         if ( !_model.IsConnectedToDevice )
         {
            Logger.Log( "Starting device scan" );
            await _client.StopScanningAsync();
            await _client.StartScanningAsync();
            _scanning = true;
         }
         else
         {
            Logger.Log( "Device found on initial connection" );
         }
      }
      catch
      {
         Logger.LogRequestFail();
      }
   }

   private void OnServerDisconnect( object sender, EventArgs e ) => _model.IsConnectedToServer = false;

   private async void OnDeviceAdded( object sender, DeviceAddedEventArgs e )
   {
      _model.DeviceName = e.Device.Name;
      _device = e.Device;

      if ( _scanning )
      {
         Logger.Log( "Device connected, stopping scan" );
         await _client.StopScanningAsync();
         _scanning = false;
      }
   }

   private async void OnDeviceRemoved( object sender, DeviceRemovedEventArgs e )
   {
      _model.DeviceName = string.Empty;
      _device = null;

      if ( !_scanning && _client.Connected )
      {
         Logger.Log( "Device disconnected, starting scan" );
         await _client.StartScanningAsync();
      }
   }

   //ISyncTarget
   public bool CanSync => _client.Connected && _device is not null;

   public Task<bool> SetupSyncAsync( string scriptFilePath, bool forceUpload )
   {
      _scriptPlayer = new ScriptPlayer( _device, Funscript.Load( scriptFilePath, _model.Offset, _model.Intensity / 100.0 ) );
      return Task.FromResult( true );
   }

   public async Task StartSyncAsync( long time ) => await _scriptPlayer.StartAsync( time );

   public async Task StopSyncAsync() => await _scriptPlayer.StopAsync();

   public async Task CleanupAsync()
   {
      await _scriptPlayer.DisposeAsync();
      _scriptPlayer = null;
   }
}
