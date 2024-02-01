using System.Text.Json.Serialization;
using VlcScriptPlayer.Buttplug;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI;

internal sealed class MainViewModel : ViewModelBase
{
   public HandyViewModel HandyVm { get; init; } = new();
   public ButtplugViewModel ButtplugVm { get; init; } = new();
   public FilterViewModel FilterVm { get; init; } = new();
   public ScriptViewModel ScriptVm { get; init; } = new();
   public PlaybackViewModel PlaybackVm { get; init; } = new();

   [JsonIgnore]
   public RelayCommand UploadScriptAndLaunchPlayerCommand { get; set; }
}
