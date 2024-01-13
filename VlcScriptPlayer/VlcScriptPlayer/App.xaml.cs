using LibVLCSharp.Shared;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal sealed partial class App : System.IDisposable
{
   private readonly SingleInstance _singleInstance = new( "VlcScriptPlayer", listenForOtherInstances: false );

   public App()
   {
      DispatcherUnhandledException += OnUnhandledException;
      if ( !_singleInstance.Claim() )
      {
         Shutdown();
      }
   }

   public void Dispose() => _singleInstance.Dispose();

   protected override void OnStartup( StartupEventArgs e )
   {
      Core.Initialize();
      new Main().Start();
   }

   protected override void OnExit( ExitEventArgs e ) => Dispose();

   private void OnUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e ) => File.WriteAllText( "crash.txt", e.Exception.ToString() );
}
