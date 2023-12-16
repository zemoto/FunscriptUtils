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
      _model.ConnectCommand = new RelayCommand( () => _ = ConnectToHandyAsync() );
      _model.SetOffsetCommand = new RelayCommand( () => _ = SetHandyOffsetAsync() );
      _model.SetRangeCommand = new RelayCommand( () => _ = SetHandyRangeAsync() );
   }

   public void Dispose() => _handyApi.Dispose();

   public async Task<(double, double)> GetHandyRangeAsync() => await _handyApi.GetRangeAsync();

   private async Task ConnectToHandyAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _model.IsConnected = false;
      if ( !await _handyApi.ConnectToAndSetupHandyAsync( _model ) )
      {
         return;
      }

      _model.CurrentOffset = _model.DesiredOffset;
      _model.CurrentSlideMin = _model.DesiredSlideMin;
      _model.CurrentSlideMax = _model.DesiredSlideMax;
      _model.IsConnected = true;
   }

   private async Task SetHandyOffsetAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetOffsetAsync( _model.DesiredOffset ) )
      {
         _model.CurrentOffset = _model.DesiredOffset;
      }
   }

   private async Task SetHandyRangeAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax ) )
      {
         _model.CurrentSlideMin = _model.DesiredSlideMin;
         _model.CurrentSlideMax = _model.DesiredSlideMax;
      }
   }

   //ISyncTarget
   public bool CanSync => _model.IsConnected;

   public async Task<bool> SetupSyncAsync( Funscript script )
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );
      return await _handyApi.UploadScriptAsync( script );
   }

   public async Task StartSyncAsync( long time ) => await _handyApi.PlayScriptAsync( time );

   public async Task StopSyncAsync() => await _handyApi.StopScriptAsync();

   public async Task CleanupAsync( bool syncSetupSuccessful )
   {
      if ( syncSetupSuccessful )
      {
         await StopSyncAsync();
         (_model.CurrentSlideMin, _model.CurrentSlideMax) = await GetHandyRangeAsync();
      }
   }
}
