using System;
using System.Threading.Tasks;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class HandyManager : IDisposable
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

   private async Task ConnectToHandyAsync()
   {
#if !TESTINGPLAYER
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      _model.IsConnected = false;
      if ( !await _handyApi.ConnectToAndSetupHandyAsync( _model ).ConfigureAwait( false ) )
      {
         return;
      }

      _model.CurrentOffset = _model.DesiredOffset;
      _model.CurrentSlideMin = _model.DesiredSlideMin;
      _model.CurrentSlideMax = _model.DesiredSlideMax;
#endif

      _model.IsConnected = true;
   }

   private async Task SetHandyOffsetAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetOffsetAsync( _model.DesiredOffset ).ConfigureAwait( false ) )
      {
         _model.CurrentOffset = _model.DesiredOffset;
      }
   }

   private async Task SetHandyRangeAsync()
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );

      if ( await _handyApi.SetRangeAsync( _model.DesiredSlideMin, _model.DesiredSlideMax ).ConfigureAwait( false ) )
      {
         _model.CurrentSlideMin = _model.DesiredSlideMin;
         _model.CurrentSlideMax = _model.DesiredSlideMax;
      }
   }

   public async Task SyncLocalRangeWithDeviceRangeAsync() => (_model.CurrentSlideMin, _model.CurrentSlideMax) = await _handyApi.GetRangeAsync().ConfigureAwait( false );

   public async Task<bool> UploadScriptAsync( string scriptFilePath, bool forceUploadScript )
   {
      _model.RequestInProgress = true;
      using var _ = new ScopeGuard( () => _model.RequestInProgress = false );
      return await _handyApi.UploadScriptAsync( scriptFilePath, forceUploadScript ).ConfigureAwait( true );
   }

   public async Task PlayScriptAsync( long time ) => await _handyApi.PlayScriptAsync( time ).ConfigureAwait( false );

   public async Task StopScriptAsync() => await _handyApi.StopScriptAsync().ConfigureAwait( false );
}
