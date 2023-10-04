namespace VlcScriptPlayer.UI;

internal sealed partial class MainWindow
{
   public MainWindow( MainViewModel model )
   {
      DataContext = model;
      InitializeComponent();
   }

   private void OnLogTextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e ) => LogTextBox.ScrollToEnd();
}
