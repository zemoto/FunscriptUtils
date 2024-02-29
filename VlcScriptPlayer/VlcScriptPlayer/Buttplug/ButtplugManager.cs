using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using System;
using System.Threading.Tasks;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ButtplugManager : ISyncTarget, IAsyncDisposable
{
   private readonly ButtplugViewModel _model;
   private readonly ButtplugClient _client;

   private readonly ScriptPlayer _scriptPlayer = new();

   private bool _scanning;

   public ButtplugManager( ButtplugViewModel model )
   {
      _model = model;
      _model.ConnectToServerCommand = new RelayCommand( async () => await ConnectToServerAsync() );

      _client = new ButtplugClient( "VlcScriptPlayer" );
      _client.DeviceAdded += OnDeviceAdded;
      _client.DeviceRemoved += OnDeviceRemoved;
      _client.ServerDisconnect += OnServerDisconnect;
   }

   public async ValueTask DisposeAsync()
   {
      if ( _client.Connected )
      {
         await _client.DisconnectAsync();
      }

      _client.Dispose();
      await _scriptPlayer.DisposeAsync();
   }

   private async Task ConnectToServerAsync()
   {
      _model.IsConnectedToServer = false;
      _model.DeviceName = string.Empty;

      const string serverUri = "ws://localhost:12345";
      var connector = new ButtplugWebsocketConnector( new Uri( serverUri ) );
      Logger.Log( "Connecting to Intiface Server" );
      try
      {
         await _client.ConnectAsync( connector );
         Logger.Log( "Connection to server successful" );
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
         Logger.LogError( "Failed to find server" );
      }
   }

   private void OnServerDisconnect( object sender, EventArgs e ) => _model.IsConnectedToServer = false;

   private async void OnDeviceAdded( object sender, DeviceAddedEventArgs e )
   {
      _model.DeviceName = e.Device.Name;
      _scriptPlayer.SetDevice( e.Device );

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
      _scriptPlayer.SetDevice( null );

      if ( !_scanning && _client.Connected )
      {
         Logger.Log( "Device disconnected, starting scan" );
         await _client.StartScanningAsync();
      }
   }

   //ISyncTarget
   public bool CanSync => _client.Connected && _model.IsConnectedToDevice;

   public Task<bool> SetupSyncAsync( Funscript script )
   {
      var actions = new VibrationActionGenerator( script, _model.Offset, _model.Intensity / 100.0 ).VibrationActions;
      _scriptPlayer.SetActions( actions );
      return Task.FromResult( true );
   }

   public Task StartSyncAsync( long time )
   {
      _scriptPlayer.Start( time );
      return Task.CompletedTask;
   }

   public async Task StopSyncAsync() => await _scriptPlayer.StopAsync();

   public async Task CleanupAsync( bool syncSetupSuccessful )
   {
      if ( syncSetupSuccessful )
      {
         await _scriptPlayer.StopAsync();
      }

      _scriptPlayer.SetActions( null );
   }
}
