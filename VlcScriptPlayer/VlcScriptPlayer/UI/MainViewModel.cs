using CommunityToolkit.Mvvm.Input;
using System.Text.Json.Serialization;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;

namespace VlcScriptPlayer.UI;

internal sealed class MainViewModel
{
   public HandyViewModel HandyVm { get; init; } = new();
   public FilterViewModel FilterVm { get; init; } = new();
   public ScriptViewModel ScriptVm { get; init; } = new();
   public PlaybackSettingsViewModel PlaybackVm { get; init; } = new();

   [JsonIgnore]
   public RelayCommand UploadScriptAndLaunchPlayerCommand { get; set; }
}
