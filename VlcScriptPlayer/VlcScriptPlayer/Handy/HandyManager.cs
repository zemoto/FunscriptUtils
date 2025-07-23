using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ZemotoCommon;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyManager : IDisposable
{
   private readonly HttpClient _client = new();
   private readonly HandyApi _handyApi;
   private readonly HandyViewModel _model;

   public HandyManager( HandyViewModel model )
   {
      _handyApi = new HandyApi( _client );

      _model = model;
      _model.ConnectCommand = new RelayCommand( async () => await ConnectToHandyAsync() );
      _model.SetOffsetCommand = new RelayCommand( async () => await SetHandyOffsetAsync() );
      _model.SetRangeCommand = new RelayCommand( async () => await SetHandyRangeAsync() );
   }

   public void Dispose() => _client.Dispose();

   public async Task<(double, double)> GetHandyRangeAsync() => await _handyApi.GetRangeAsync();

   private async Task ConnectToHandyAsync()
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      _model.IsConnected = await _handyApi.ConnectToAndSetupHandyAsync( _model.ConnectionId );
   }

   private async Task SetHandyOffsetAsync()
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      await _handyApi.SetOffsetAsync( _model.DesiredOffset );
   }

   private async Task SetHandyRangeAsync()
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      await _handyApi.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax );
   }

   public bool IsConnected => _model.IsConnected;

   public async Task<bool> SetupSyncAsync( Funscript script )
   {
      if ( !_model.IsConnected )
      {
         return true;
      }

      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      if ( _model.SetOptionsWhenSyncing )
      {
         await _handyApi.SetOffsetAsync( _model.DesiredOffset );
         await _handyApi.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax );
      }

      return await _handyApi.UploadScriptAsync( script );
   }

   public async Task UpdateScriptAsync( Funscript script ) => await _handyApi.UploadScriptAsync( script );

   public async Task StartSyncAsync( long time ) => await _handyApi.PlayScriptAsync( time );

   public async Task StopSyncAsync() => await _handyApi.StopScriptAsync();

   public async Task SyncTimeAsync( long time ) => await _handyApi.SyncTimeAsync( time );
}