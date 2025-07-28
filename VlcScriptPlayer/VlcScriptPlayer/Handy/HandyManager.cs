using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using ZemotoCommon;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyManager : IDisposable
{
   private readonly HandyApi _api = new();
   private readonly HandyViewModel _model;

   public HandyManager( HandyViewModel model )
   {
      _model = model;
      _model.ConnectCommand = new RelayCommand( async () => await ConnectToHandyAsync() );
      _model.SetOffsetCommand = new RelayCommand( async () => await SetHandyOffsetAsync() );
      _model.SetRangeCommand = new RelayCommand( async () => await SetHandyRangeAsync() );
   }

   public void Dispose() => _api.Dispose();

   public async Task<(double, double)> GetHandyRangeAsync() => await _api.GetRangeAsync();

   private async Task ConnectToHandyAsync()
   {
      using var _ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      _model.IsConnected = await _api.ConnectToAndSetupHandyAsync( _model.ConnectionId );
   }

   private async Task SetHandyOffsetAsync()
   {
      using var __ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      _ = await _api.SetOffsetAsync( _model.DesiredOffset );
   }

   private async Task SetHandyRangeAsync()
   {
      using var __ = new ScopeGuard( () => _model.RequestInProgress = true, () => _model.RequestInProgress = false );
      _ = await _api.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax );
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
         if ( !await _api.SetOffsetAsync( _model.DesiredOffset ) ||
              !await _api.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax ) )
         {
            return false;
         }
      }

      return await _api.UploadScriptAsync( script );
   }

   public async Task UpdateScriptAsync( Funscript script ) => await _api.UploadScriptAsync( script );

   public async Task StartSyncAsync( long time ) => await _api.PlayScriptAsync( time );

   public async Task StopSyncAsync() => await _api.StopScriptAsync();
}