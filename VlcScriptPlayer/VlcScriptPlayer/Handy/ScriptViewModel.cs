using System.Text.Json.Serialization;
using System.Windows.Input;
using ZemotoCommon;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Handy;

internal sealed class ScriptViewModel : ViewModelBase
{
   private string _videoFilePath = string.Empty;
   public string VideoFilePath
   {
      get => _videoFilePath;
      set
      {
         if ( SetProperty( ref _videoFilePath, value ) )
         {
            OnPropertyChanged( nameof( DisplayedVideoFilePath ) );
         }
      }
   }

   public string DisplayedVideoFilePath => FileUtils.AbbreviatePath( _videoFilePath );

   private string _scriptFilePath = string.Empty;
   public string ScriptFilePath
   {
      get => _scriptFilePath;
      set
      {
         if ( SetProperty( ref _scriptFilePath, value ) )
         {
            OnPropertyChanged( nameof( DisplayedScriptFilePath ) );
         }
      }
   }

   public string DisplayedScriptFilePath => FileUtils.AbbreviatePath( _scriptFilePath );

   private string _scriptFolder = string.Empty;
   public string ScriptFolder
   {
      get => _scriptFolder;
      set => SetProperty( ref _scriptFolder, value );
   }

   [JsonIgnore]
   public ICommand SelectVideoCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptFolderCommand { get; set; }
}
