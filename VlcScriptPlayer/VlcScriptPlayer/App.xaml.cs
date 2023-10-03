﻿namespace VlcScriptPlayer;

internal sealed partial class App : System.IDisposable
{
   private Main _main;
   private readonly ZemotoCommon.SingleInstance _singleInstance = new( "VlcScriptPlayer", listenForOtherInstances: false );

   public App()
   {
      DispatcherUnhandledException += OnUnhandledException;
      if ( !_singleInstance.Claim() )
      {
         Shutdown();
      }
   }

   public void Dispose()
   {
      _main.Dispose();
      _singleInstance.Dispose();
   }

   protected override void OnStartup( System.Windows.StartupEventArgs e )
   {
      LibVLCSharp.Shared.Core.Initialize();

      _main = new Main();
      _main.Start();
   }

   protected override void OnExit( System.Windows.ExitEventArgs e ) => Dispose();

   private void OnUnhandledException( object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e )
   {
      System.IO.File.WriteAllText( "crash.txt", e.Exception.ToString() );
   }
}
