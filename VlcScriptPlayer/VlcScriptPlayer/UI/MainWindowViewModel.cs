using System.Windows.Input;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer.UI;

internal sealed class MainWindowViewModel : Config, IFilterConfig
{
   public MainWindowViewModel() => PropertyChanged += OnPropertyChanged;

   private void OnPropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
   {
      if ( e.PropertyName is nameof( IsConnected ) or nameof( VideoFilePath ) or nameof( ScriptFilePath ) )
      {
         OnPropertyChanged( nameof( ScriptAndVideoReady ) );
      }
   }

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

   public bool ScriptAndVideoReady => !string.IsNullOrEmpty( VideoFilePath ) && !string.IsNullOrEmpty( ScriptFilePath ) && IsConnected;

   private string _selectedScriptFilePath;
   public string SelectedScriptFilePath
   {
      get => _selectedScriptFilePath;
      set => SetProperty( ref _selectedScriptFilePath, value );
   }

   public ICommand ConnectCommand { get; set; }
   public ICommand SetOffsetCommand { get; set; }
   public ICommand SelectVideoCommand { get; set; }
   public ICommand SelectScriptCommand { get; set; }
   public ICommand AddScriptFolderCommand { get; set; }
   public ICommand RemoveScriptFolderCommand { get; set; }
   public ICommand UploadScriptCommand { get; set; }
}
