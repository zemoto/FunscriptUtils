using System.Windows.Input;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer.UI;

internal sealed class MainWindowViewModel : Config, IFilterConfig
{
   private bool _isConnected;
   public bool IsConnected
   {
      get => _isConnected;
      set => SetProperty( ref _isConnected, value );
   }

   private int _currentOffset;
   public int CurrentOffset
   {
      get => _currentOffset;
      set => SetProperty( ref _currentOffset, value );
   }

   private bool _forceUploadScript;
   public bool ForceUploadScript
   {
      get => _forceUploadScript;
      set => SetProperty( ref _forceUploadScript, value );
   }

   private bool _requestInProgress;
   public bool RequestInProgress
   {
      get => _requestInProgress;
      set => SetProperty( ref _requestInProgress, value );
   }

   public ICommand ConnectCommand { get; set; }
   public ICommand SetOffsetCommand { get; set; }
   public ICommand SelectVideoCommand { get; set; }
   public ICommand SelectScriptCommand { get; set; }
   public ICommand SelectScriptFolderCommand { get; set; }
   public ICommand UploadScriptAndLaunchPlayerCommand { get; set; }
}
