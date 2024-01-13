using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class ScriptManager
{
   private readonly ScriptViewModel _model;

   public ScriptManager( ScriptViewModel model )
   {
      _model = model;
      _model.SelectVideoCommand = new RelayCommand( SelectVideo );
      _model.SelectScriptCommand = new RelayCommand( SelectScript );
      _model.SelectScriptFolderCommand = new RelayCommand( SelectScriptFolder );
      VerifyPaths();
   }

   public bool VerifyPaths()
   {
      bool pathsValid = true;
      if ( !File.Exists( _model.VideoFilePath ) )
      {
         _model.VideoFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !File.Exists( _model.ScriptFilePath ) )
      {
         _model.ScriptFilePath = string.Empty;
         pathsValid = false;
      }

      if ( !string.IsNullOrEmpty( _model.ScriptFolder ) && !Directory.Exists( _model.ScriptFolder ) )
      {
         _model.ScriptFolder = string.Empty;
      }

      return pathsValid;
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

      _model.VideoFilePath = dlg.FileName;

      var videoFolderPath = Path.GetDirectoryName( dlg.FileName );
      var fileName = Path.GetFileNameWithoutExtension( dlg.FileName );
      foreach ( var folder in new string[2] { videoFolderPath, _model.ScriptFolder } )
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
            _model.ScriptFilePath = matchingScript;
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
         _model.ScriptFilePath = dlg.FileName;
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
