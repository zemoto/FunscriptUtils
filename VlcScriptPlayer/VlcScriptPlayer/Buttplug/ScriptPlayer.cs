using Buttplug.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZemotoCommon;

namespace VlcScriptPlayer.Buttplug;

internal sealed class ScriptPlayer : IAsyncDisposable
{
   private readonly Funscript _script;
   private readonly ButtplugClientDevice _device;
   private readonly SemaphoreSlim _stopSemaphore = new( 1, 1 );

   private CancellationTokenSource _cancelTokenSource;
   private Task _scriptTask;

   public ScriptPlayer( ButtplugClientDevice device, Funscript script )
   {
      _device = device;
      _script = script;
   }

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

   private async Task ScriptTask( object timeOffsetObj )
   {
      long timeOffset = (long)timeOffsetObj;
      var startDateTime = DateTime.Now;
      while ( true )
      {
         var currentDateTime = DateTime.Now;
         var currentScriptTime = (long)( currentDateTime - startDateTime ).TotalMilliseconds + timeOffset;
         var nextAction = _script.VibrationActions.Find( x => x.Time >= currentScriptTime );

         if ( _cancelTokenSource.IsCancellationRequested || nextAction is null )
         {
            await _device.Stop();
            return;
         }

         await Task.Delay( (int)( nextAction.Time - currentScriptTime ) );

         await _device.VibrateAsync( nextAction.Intensity );
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
      await _device.Stop();
      await _scriptTask;

      _cancelTokenSource?.Dispose();
      _cancelTokenSource = null;
      _scriptTask = null;
   }
}
