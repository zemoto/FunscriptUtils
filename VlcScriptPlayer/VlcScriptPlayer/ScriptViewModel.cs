using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal sealed class ScriptViewModel : ObservableObject
{
   public void ReloadScript() => _script = null;

   private SystemFile _videoFile;
   public SystemFile VideoFile
   {
      get => _videoFile;
      set => SetProperty( ref _videoFile, value );
   }

   private SystemFile _scriptFile;
   public SystemFile ScriptFile
   {
      get => _scriptFile;
      set
      {
         if ( SetProperty( ref _scriptFile, value ) )
         {
            _script = null;
         }
      }
   }

   private Funscript _script;
   public Funscript Script => _script ??= _scriptFile.DeserializeContents<Funscript>();

   private string _scriptFolder = string.Empty;
   public string ScriptFolder
   {
      get => _scriptFolder;
      set => SetProperty( ref _scriptFolder, value );
   }

   private bool _notifyOnScriptFileModified;
   public bool NotifyOnScriptFileModified
   {
      get => _notifyOnScriptFileModified;
      set => SetProperty( ref _notifyOnScriptFileModified, value );
   }

   [JsonIgnore]
   public ICommand SelectVideoCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptFolderCommand { get; set; }
}
