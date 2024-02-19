namespace VlcScriptPlayer.Vlc;

internal sealed partial class AdvancedPlaybackSettingsWindow
{
   public AdvancedPlaybackSettingsWindow( PlaybackSettingsViewModel viewModel )
   {
      DataContext = viewModel;
      InitializeComponent();
   }

   private void OnOkClicked( object sender, System.Windows.RoutedEventArgs e ) => Close();
}
