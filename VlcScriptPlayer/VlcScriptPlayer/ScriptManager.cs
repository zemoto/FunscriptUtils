using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using ZemotoCommon.UI;

namespace VlcScriptPlayer;

internal sealed class ScriptManager
{
   public ScriptViewModel Model { get; }

   public event EventHandler ScriptChanged;

   public ScriptManager( ScriptViewModel model )
   {
      Model = model;
      Model.SelectVideoCommand = new RelayCommand( SelectVideo );
      Model.SelectScriptCommand = new RelayCommand( SelectScript );
      Model.SelectScriptFolderCommand = new RelayCommand( SelectScriptFolder );
      _ = VerifyPaths();
   }

   public bool VerifyPaths()
   {
      bool pathsValid = true;
      if ( !File.Exists( Model.VideoFilePath ) )
      {
         Model.VideoFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !File.Exists( Model.ScriptFilePath ) )
      {
         Model.ScriptFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !string.IsNullOrEmpty( Model.ScriptFolder ) && !Directory.Exists( Model.ScriptFolder ) )
      {
         Model.ScriptFolder = string.Empty;
      }

      return pathsValid;
   }

   public void OpenSelectedScriptInEditor()
   {
      if ( VerifyPaths() )
      {
         _ = Process.Start( "explorer", $"\"{Model.ScriptFilePath}\"" );
      }
   }

   public void NotifyScriptChanged()
   {
      if ( VerifyPaths() )
      {
         Model.ReloadScript();
         ScriptChanged?.Invoke( this, EventArgs.Empty );
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

      Model.VideoFilePath = dlg.FileName;

      var videoFolderPath = Path.GetDirectoryName( dlg.FileName );
      var fileName = Path.GetFileNameWithoutExtension( dlg.FileName );
      foreach ( var folder in new string[2] { videoFolderPath, Model.ScriptFolder } )
      {
         if ( string.IsNullOrEmpty( folder ) )
         {
            continue;
         }

         Logger.Log( $"Searching folder for script: {folder}" );
         var scripts = Directory.GetFiles( folder, "*.funscript" ).Concat( Directory.GetFiles( folder, "*.csv" ) ).ToArray();
         var matchingScript = Array.Find( scripts, x => Path.GetFileNameWithoutExtension( x ).Equals( fileName, StringComparison.Ordinal ) );
         if ( !string.IsNullOrWhiteSpace( matchingScript ) )
         {
            Model.ScriptFilePath = matchingScript;
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
         Model.ScriptFilePath = dlg.FileName;
      }
   }

   private void SelectScriptFolder()
   {
      var dlg = new VistaFolderBrowserDialog() { Multiselect = false };
      if ( dlg.ShowDialog( Application.Current.MainWindow ) == true )
      {
         Model.ScriptFolder = dlg.SelectedPath;
      }
   }
}
