namespace VlcScriptPlayer;

internal sealed partial class App
{
   private Main _main;

   protected override void OnStartup( System.Windows.StartupEventArgs e )
   {
      LibVLCSharp.Shared.Core.Initialize();

      _main = new Main();
      _main.Start();
   }

   protected override void OnExit( System.Windows.ExitEventArgs e )
   {
      _main.Dispose();
   }
}
