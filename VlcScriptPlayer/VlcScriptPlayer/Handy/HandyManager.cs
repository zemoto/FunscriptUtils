using System;
using System.Threading.Tasks;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyManager : ISyncTarget, IDisposable
{
   private readonly HandyApi _handyApi = new();
   private readonly HandyViewModel _model;

   public HandyManager( HandyViewModel model )
   {
      _model = model;
      _model.ConnectCommand = new RelayCommand( async () => await ConnectToHandyAsync() );
      _model.SetOffsetCommand = new RelayCommand( async () => await SetHandyOffsetAsync() );
      _model.SetRangeCommand = new RelayCommand( async () => await SetHandyRangeAsync() );
   }

   public void Dispose() => _handyApi.Dispose();

   public async Task<(double, double)> GetHandyRangeAsync() => await _handyApi.GetRangeAsync();

   private async Task ConnectToHandyAsync()
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );

      _model.IsConnected = false;
      if ( !await _handyApi.ConnectToAndSetupHandyAsync( _model.ConnectionId ) )
      {
         return;
      }

      _model.IsConnected = true;
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

   //ISyncTarget
   public bool CanSync => _model.IsConnected;

   public async Task<bool> SetupSyncAsync( Funscript script )
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );

      if ( _model.SetOptionsWhenSyncing )
      {
         await _handyApi.SetOffsetAsync( _model.DesiredOffset );
         await _handyApi.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax );
      }

      return await _handyApi.UploadScriptAsync( script );
   }

   public async Task StartSyncAsync( long time ) => await _handyApi.PlayScriptAsync( time );

   public async Task StopSyncAsync() => await _handyApi.StopScriptAsync();

   public Task CleanupAsync( bool syncSetupSuccessful ) => Task.CompletedTask;
}
