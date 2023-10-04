namespace VlcScriptPlayer;

internal sealed partial class App : System.IDisposable
{
   private readonly ZemotoCommon.SingleInstance _singleInstance = new( "VlcScriptPlayer", listenForOtherInstances: false );

   public App()
   {
      DispatcherUnhandledException += OnUnhandledException;
      if ( !_singleInstance.Claim() )
      {
         Shutdown();
      }
   }

   public void Dispose() => _singleInstance.Dispose();

   protected override void OnStartup( System.Windows.StartupEventArgs e )
   {
      LibVLCSharp.Shared.Core.Initialize();
      new Main().Start();
   }

   protected override void OnExit( System.Windows.ExitEventArgs e ) => Dispose();

   private void OnUnhandledException( object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e ) => System.IO.File.WriteAllText( "crash.txt", e.Exception.ToString() );
}
