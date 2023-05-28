using System.Collections.ObjectModel;
using System.Windows.Input;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI;

internal sealed class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel( Config config )
	{
		ConnectionId = config.ConnectionId;
		DesiredOffset = config.DesiredOffset;
		ScriptFolders = new ObservableCollection<string>( config.ScriptFolders );
	}

	private string _connectionId;
	public string ConnectionId
	{
		get => _connectionId;
		set => SetProperty( ref _connectionId, value );
	}

	private bool _isConnected;
	public bool IsConnected
	{
		get => _isConnected;
		set
		{
			if ( SetProperty( ref _isConnected, value ) )
			{
				OnPropertyChanged( nameof( ScriptAndVideoReady ) );
			}
		}
	}

	private int _currentOffset;
	public int CurrentOffset
	{
		get => _currentOffset;
		set => SetProperty( ref _currentOffset, value );
	}

	private int _desiredOffset;
	public int DesiredOffset
	{
		get => _desiredOffset;
		set => SetProperty( ref _desiredOffset, value );
	}

	private string _videoFilePath;
	public string VideoFilePath
	{
		get => _videoFilePath;
		set
		{
			if ( SetProperty( ref _videoFilePath, value ) )
			{
				OnPropertyChanged( nameof( ScriptAndVideoReady ) );
			}
		}
	}

	private string _scriptFilePath;
	public string ScriptFilePath
	{
		get => _scriptFilePath;
		set
		{
			if ( SetProperty( ref _scriptFilePath, value ) )
			{
				OnPropertyChanged( nameof( ScriptAndVideoReady ) );
			}
		}
	}

	private bool _requestInProgress;
	public bool RequestInProgress
	{
		get => _requestInProgress;
		set => SetProperty( ref _requestInProgress, value );
	}

	public bool ScriptAndVideoReady => !string.IsNullOrEmpty( _videoFilePath ) && !string.IsNullOrEmpty( _scriptFilePath ) && IsConnected;

	private string _selectedScriptFilePath;

	public string SelectedScriptFilePath
	{
		get => _selectedScriptFilePath;
		set => SetProperty( ref _selectedScriptFilePath, value );
	}

	public ObservableCollection<string> ScriptFolders { get; }

	public ICommand ConnectCommand { get; set; }
	public ICommand SetOffsetCommand { get; set; }
	public ICommand SelectVideoCommand { get; set; }
	public ICommand SelectScriptCommand { get; set; }
	public ICommand AddScriptFolderCommand { get; set; }
	public ICommand RemoveScriptFolderCommand { get; set; }
	public ICommand UploadScriptCommand { get; set; }
}
