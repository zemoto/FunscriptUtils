using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer.UI;

internal sealed partial class AdvancedPlaybackSettingsWindow
{
   public AdvancedPlaybackSettingsWindow( PlaybackSettingsViewModel viewModel )
   {
      DataContext = viewModel;
      InitializeComponent();
   }

   private void OnOkClicked( object sender, System.Windows.RoutedEventArgs e ) => Close();
}
