using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZemotoCommon;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ScriptPlayer : IAsyncDisposable
{
   private readonly SemaphoreSlim _stopSemaphore = new( 1, 1 );

   private List<VibrationAction> _actions;
   private ButtplugClientDevice _device;
   private CancellationTokenSource _cancelTokenSource;
   private Task _scriptTask;

   public void SetActions( List<VibrationAction> actions ) => _actions = actions;

   public void SetDevice( ButtplugClientDevice device ) => _device  = device;

   public async ValueTask DisposeAsync()
   {
      await StopAsync();
      _stopSemaphore.Dispose();
   }

   public void Start( long startTime ) => _scriptTask ??= ScriptTask( startTime );

   private async Task ScriptTask( long timeOffset )
   {
      _cancelTokenSource = new CancellationTokenSource();
      var startDateTime = DateTime.Now;
      try
      {
         while ( true )
         {
            var currentDateTime = DateTime.Now;
            var currentScriptTime = (long)( currentDateTime - startDateTime ).TotalMilliseconds + timeOffset;
            var nextAction = _actions.Find( x => x.Time >= currentScriptTime );

            if ( _cancelTokenSource.IsCancellationRequested || nextAction is null )
            {
               return;
            }

            await Task.Delay( (int)( nextAction.Time - currentScriptTime ), _cancelTokenSource.Token );

            if ( !await VibrateDeviceAsync( nextAction.Intensity ) )
            {
               return;
            }
         }
      }
      catch { }
      finally
      {
         _cancelTokenSource?.Dispose();
         _cancelTokenSource = null;
         _scriptTask = null;
         await StopDeviceAsync();
      }
   }

   public async Task StopAsync()
   {
      await _stopSemaphore.WaitAsync();
      using var _ = new ScopeGuard( () => _stopSemaphore.Release() );
      if ( _scriptTask is null )
      {
         return;
      }

      _cancelTokenSource.Cancel();
      await _scriptTask;
   }

   private async Task<bool> StopDeviceAsync()
   {
      if ( _device is null )
      {
         return false;
      }

      try
      {
         await _device.Stop();
         return true;
      }
      catch
      {
         _device = null;
         return false;
      }
   }

   private async Task<bool> VibrateDeviceAsync( double intensity )
   {
      if ( _device is null )
      {
         return false;
      }

      try
      {
         await _device.VibrateAsync( intensity );
         return true;
      }
      catch
      {
         _device = null;
         return false;
      }
   }
}
