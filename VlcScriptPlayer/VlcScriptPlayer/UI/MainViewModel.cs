using CommunityToolkit.Mvvm.Input;
using System.Text.Json.Serialization;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;
using VlcScriptPlayer.Vlc.Filter;
using ZemotoCommon;

namespace VlcScriptPlayer.UI;

internal sealed class MainViewModel
{
   private static readonly SystemFile _configFile = new( "config.json" );

   public static MainViewModel ReadFromFile() => _configFile.DeserializeContents<MainViewModel>() ?? new MainViewModel();

   public void SaveToFile() => _configFile.SerializeInto( this );

   public HandyViewModel HandyVm { get; init; } = new();
   public FilterViewModel FilterVm { get; init; } = new();
   public ScriptViewModel ScriptVm { get; init; } = new();
   public PlaybackSettingsViewModel PlaybackVm { get; init; } = new();

   [JsonIgnore]
   public RelayCommand UploadScriptAndLaunchPlayerCommand { get; set; }
}
