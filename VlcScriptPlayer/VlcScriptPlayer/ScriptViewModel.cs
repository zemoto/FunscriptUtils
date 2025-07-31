using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using ZemotoCommon;

namespace VlcScriptPlayer;

internal sealed partial class ScriptViewModel : ObservableObject
{
   [ObservableProperty]
   private SystemFile _videoFile;

   [ObservableProperty]
   private SystemFile _scriptFile;

   [ObservableProperty]
   private Funscript _script;

   [ObservableProperty]
   private string _scriptFolder = string.Empty;

   [ObservableProperty]
   private bool _notifyOnScriptFileModified;

   [JsonIgnore]
   public ICommand SelectVideoCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptCommand { get; set; }

   [JsonIgnore]
   public ICommand SelectScriptFolderCommand { get; set; }
}
