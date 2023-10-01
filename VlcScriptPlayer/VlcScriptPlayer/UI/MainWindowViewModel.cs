using System.Windows.Input;
using VlcScriptPlayer.Config;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI;

internal sealed class MainWindowViewModel : ViewModelBase
{
   public MainWindowViewModel() => Config = new AppConfig(); // Design-time

   public MainWindowViewModel( AppConfig config ) => Config = config;

   public AppConfig Config { get; }

   private bool _requestInProgress;
   public bool RequestInProgress
   {
      get => _requestInProgress;
      set => SetProperty( ref _requestInProgress, value );
   }

   public ICommand ConnectCommand { get; set; }
   public ICommand SetOffsetCommand { get; set; }
   public ICommand SetRangeCommand { get; set; }
   public ICommand SelectVideoCommand { get; set; }
   public ICommand SelectScriptCommand { get; set; }
   public ICommand SelectScriptFolderCommand { get; set; }
   public ICommand UploadScriptAndLaunchPlayerCommand { get; set; }
}
