namespace VlcScriptPlayer;

internal sealed partial class App : ZemotoCommon.UI.CommonApp
{
   public App()
      : base( "VlcScriptPlayer", listenForOtherInstances: false )
   {
   }

   protected override void OnStartup( System.Windows.StartupEventArgs e )
   {
      LibVLCSharp.Shared.Core.Initialize();
      new Main().Start();
   }
}