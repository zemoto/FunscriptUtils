using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace VlcScriptPlayer.Vlc;

internal sealed partial class PlaybackSettingsViewModel : ObservableObject
{
   [property: JsonIgnore]
   [ObservableProperty]
   private List<string> _audioOutputs;
   partial void OnAudioOutputsChanged( List<string> value ) => SelectedAudioOutput = value.FirstOrDefault();

   [ObservableProperty]
   private string _selectedAudioOutput;

   [ObservableProperty]
   private uint _cacheSize = 3000;

   [ObservableProperty]
   private bool _UseHardwareDecoding = true;

   [ObservableProperty]
   private bool _loop;

   [ObservableProperty]
   private bool _autoplay;

   [JsonIgnore]
   public RelayCommand ShowAdvancedPlaybackSettingsCommand { get; set; }
}