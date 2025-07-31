using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal sealed class ScriptManager : IDisposable
{
   public readonly ScriptViewModel _model;
   private readonly FileSystemWatcher _scriptFileWatcher;

   private Process _scriptFileEditorProcess;
   private SystemFile _originalScriptFile;
   private bool _tempScriptFileChanged;

   public event EventHandler ScriptChanged;

   public Funscript Script => _model.Script;

   public ScriptManager( ScriptViewModel model )
   {
      _model = model;
      _model.SelectVideoCommand = new RelayCommand( SelectVideo );
      _model.SelectScriptCommand = new RelayCommand( SelectScript );
      _model.SelectScriptFolderCommand = new RelayCommand( SelectScriptFolder );
      _ = VerifyPaths();

      _model.PropertyChanged += OnPropertyChanged;

      _scriptFileWatcher = new FileSystemWatcher { NotifyFilter = NotifyFilters.LastWrite };
      UpdateScriptWatcher();

      _scriptFileWatcher.Changed += OnScriptFileChanged;
   }

   public void Dispose() => _scriptFileWatcher.Dispose();

   public bool VerifyPaths()
   {
      bool pathsValid = true;
      if ( _model.VideoFile is not null && !_model.VideoFile.Exists() )
      {
         Logger.LogError( $"Video file not found: {_model.VideoFile.FullPath}" );
         _model.VideoFile = null;
         pathsValid = false;
      }

      if ( _model.ScriptFile is not null && !_model.ScriptFile.Exists() )
      {
         Logger.LogError( $"Script file not found: {_model.ScriptFile.FullPath}" );
         _model.ScriptFile = null;
         pathsValid = false;
      }

      if ( !string.IsNullOrEmpty( _model.ScriptFolder ) && !Directory.Exists( _model.ScriptFolder ) )
      {
         _model.ScriptFolder = string.Empty;
      }

      return pathsValid;
   }

   public bool LoadScript()
   {
      if ( !_model.ScriptFile.Exists() )
      {
         return true;
      }

      try
      {
         _model.Script = _model.ScriptFile.DeserializeContents<Funscript>();
         return true;
      }
      catch ( JsonException ex )
      {
         Logger.LogError( $"Invalid value in script on line {ex.LineNumber} position {ex.BytePositionInLine}" );
         return false;
      }
   }

   public void OpenSelectedScriptInEditor()
   {
      if ( _scriptFileEditorProcess is not null )
      {
         _ = NativeMethods.SetForegroundWindow( _scriptFileEditorProcess.MainWindowHandle );
         return;
      }

      if ( !_model.ScriptFile.Exists() || !UtilityMethods.GetDefaultAppForExtension( _model.ScriptFile.Extension, out var editorExe ) )
      {
         return;
      }

      if ( _originalScriptFile is null && !NameAndDirectoryMatch( _model.ScriptFile, _model.VideoFile ) )
      {
         if ( _model.ScriptFile.CopyTo( _model.VideoFile.Directory, _model.VideoFile.NameNoExtension, overwrite: false, out var tempScriptFile ) )
         {
            _originalScriptFile = _model.ScriptFile;
            _model.ScriptFile = tempScriptFile;
         }
         else
         {
            return;
         }
      }

      EditScriptAsync( editorExe );
   }

   public static bool NameAndDirectoryMatch( SystemFile l, SystemFile r ) => l.NameNoExtension.Equals( r.NameNoExtension, StringComparison.OrdinalIgnoreCase ) && l.Directory.Equals( r.Directory, StringComparison.OrdinalIgnoreCase );

   private async void EditScriptAsync( string editorExe )
   {
      await Task.Run( () =>
      {
         _scriptFileEditorProcess = Process.Start( editorExe, $"\"{_model.ScriptFile.FullPath}\"" );
         _scriptFileEditorProcess.WaitForExit();
         _scriptFileEditorProcess.Dispose();
         _scriptFileEditorProcess = null;
      } );

      if ( _originalScriptFile is not null )
      {
         if ( _tempScriptFileChanged )
         {
            if ( !_model.ScriptFile.MoveTo( _originalScriptFile.FullPath, overwrite: true, out _ ) )
            {
               _model.ScriptFile.Delete();
            }

            _model.ScriptFile = _originalScriptFile;
         }
         else
         {
            _model.ScriptFile.Delete();
            _model.ScriptFile = _originalScriptFile;
         }
      }

      _originalScriptFile = null;
      _tempScriptFileChanged = false;
   }

   public void OnVideoPlayerClosing() => _scriptFileEditorProcess?.Kill();

   private void UpdateScriptWatcher()
   {
      var scriptFile = _model.ScriptFile;
      if ( scriptFile.Exists() )
      {
         _scriptFileWatcher.Path = scriptFile.Directory;
         _scriptFileWatcher.Filter = scriptFile.Name;
         _scriptFileWatcher.EnableRaisingEvents = true;
      }
      else
      {
         _scriptFileWatcher.Filter = string.Empty;
         _scriptFileWatcher.EnableRaisingEvents = false;
      }
   }

   private void OnPropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
   {
      if ( e.PropertyName.Equals( nameof( _model.ScriptFile ), StringComparison.OrdinalIgnoreCase ) )
      {
         UpdateScriptWatcher();
      }
   }

   private void OnScriptFileChanged( object sender, FileSystemEventArgs e )
   {
      if ( _originalScriptFile is not null )
      {
         _tempScriptFileChanged = true;
      }

      if ( _model.NotifyOnScriptFileModified )
      {
         _scriptFileWatcher.EnableRaisingEvents = false;
         if ( _model.ScriptFile.Exists() && LoadScript() )
         {
            ScriptChanged?.Invoke( this, EventArgs.Empty );
         }

         _scriptFileWatcher.EnableRaisingEvents = true;
      }
   }

   private void SelectVideo()
   {
      const string filter = "Video Files (*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi)|*.mp4;*.wmv;*.webm;*.swf;*.mkv;*.avi|All files (*.*)|*.*";
      var dlg = new VistaOpenFileDialog
      {
         Filter = filter,
         Multiselect = false
      };

      if ( dlg.ShowDialog( Application.Current.MainWindow ) != true )
      {
         return;
      }

      _model.VideoFile = dlg.FileName;

      foreach ( var folder in new string[2] { _model.VideoFile.Directory, _model.ScriptFolder } )
      {
         if ( string.IsNullOrEmpty( folder ) )
         {
            continue;
         }

         Logger.Log( $"Searching folder for script: {folder}" );
         var scripts = Directory.GetFiles( folder, "*.funscript" ).Concat( Directory.GetFiles( folder, "*.csv" ) ).ToArray();
         var matchingScript = Array.Find( scripts, x => Path.GetFileNameWithoutExtension( x ).Equals( _model.VideoFile.NameNoExtension, StringComparison.OrdinalIgnoreCase ) );
         if ( !string.IsNullOrWhiteSpace( matchingScript ) )
         {
            _model.ScriptFile = matchingScript;
            Logger.Log( $"Found script: {matchingScript}" );
            return;
         }
      }
   }

   private void SelectScript()
   {
      const string filter = "Script Files (*.funscript)|*.funscript|All files (*.*)|*.*";
      var dlg = new VistaOpenFileDialog
      {
         Filter = filter,
         Multiselect = false
      };

      if ( dlg.ShowDialog( Application.Current.MainWindow ) == true )
      {
         _model.ScriptFile = dlg.FileName;
      }
   }

   private void SelectScriptFolder()
   {
      var dlg = new VistaFolderBrowserDialog() { Multiselect = false };
      if ( dlg.ShowDialog( Application.Current.MainWindow ) == true )
      {
         _model.ScriptFolder = dlg.SelectedPath;
      }
   }
}
