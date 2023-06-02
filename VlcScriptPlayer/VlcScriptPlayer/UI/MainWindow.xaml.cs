namespace VlcScriptPlayer.UI;

internal sealed partial class MainWindow
{
   public MainWindow( MainWindowViewModel model )
   {
      DataContext = model;
      InitializeComponent();
   }

   private void OnLogTextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e ) => LogTextBox.ScrollToEnd();
}
