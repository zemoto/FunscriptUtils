using Buttplug.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZemotoCommon;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ScriptPlayer : IAsyncDisposable
{
   private readonly SemaphoreSlim _stopSemaphore = new( 1, 1 );

   private Funscript _script;
   private ButtplugClientDevice _device;
   private CancellationTokenSource _cancelTokenSource;
   private Task _scriptTask;

   public void SetScript( Funscript script ) => _script = script;

   public void SetDevice( ButtplugClientDevice device ) => _device  = device;

   public async ValueTask DisposeAsync()
   {
      await StopAsync();
      _stopSemaphore.Dispose();
   }

   public async Task StartAsync( long startTime )
   {
      await StopAsync();
      _cancelTokenSource = new CancellationTokenSource();

      _scriptTask = ScriptTask( startTime );
   }

   private async Task ScriptTask( long timeOffset )
   {
      var startDateTime = DateTime.Now;
      while ( true )
      {
         var currentDateTime = DateTime.Now;
         var currentScriptTime = (long)( currentDateTime - startDateTime ).TotalMilliseconds + timeOffset;
         var nextAction = _script.VibrationActions.Find( x => x.Time >= currentScriptTime );

         if ( _cancelTokenSource.IsCancellationRequested || nextAction is null )
         {
            await StopDeviceAsync();
            return;
         }

         await Task.Delay( (int)( nextAction.Time - currentScriptTime ) );

         if ( !await VibrateDeviceAsync( nextAction.Intensity ) )
         {
            return;
         }
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
      await StopDeviceAsync();
      await _scriptTask;

      _cancelTokenSource?.Dispose();
      _cancelTokenSource = null;
      _scriptTask = null;
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
