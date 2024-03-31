using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class ScriptManager : IDisposable
{
   public readonly ScriptViewModel _model;
   private readonly FileSystemWatcher _scriptFileWatcher;

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
      if ( !_model.VideoFile.Exists() )
      {
         _model.VideoFile = null;
         pathsValid = false;
      }

      if ( !_model.ScriptFile.Exists() )
      {
         _model.ScriptFile = null;
         pathsValid = false;
      }

      if ( !string.IsNullOrEmpty( _model.ScriptFolder ) && !Directory.Exists( _model.ScriptFolder ) )
      {
         _model.ScriptFolder = string.Empty;
      }

      return pathsValid;
   }

   public void OpenSelectedScriptInEditor()
   {
      if ( _model.ScriptFile.Exists() )
      {
         _ = Process.Start( "explorer", $"\"{_model.ScriptFile}\"" );
      }
   }

   public void NotifyScriptChanged()
   {
      if ( _model.ScriptFile.Exists() )
      {
         _model.ReloadScript();
         ScriptChanged?.Invoke( this, EventArgs.Empty );
      }
   }

   private void UpdateScriptWatcher()
   {
      var scriptFile = _model.ScriptFile;
      if ( scriptFile.Exists() )
      {
         _scriptFileWatcher.Path = scriptFile.Directory;
         _scriptFileWatcher.Filter = scriptFile.FileName;
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
      if ( _model.NotifyOnScriptFileModified )
      {
         _scriptFileWatcher.EnableRaisingEvents = false;
         NotifyScriptChanged();
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
         var matchingScript = Array.Find( scripts, x => Path.GetFileNameWithoutExtension( x ).Equals( _model.VideoFile.FileNameNoExtension, StringComparison.OrdinalIgnoreCase ) );
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
