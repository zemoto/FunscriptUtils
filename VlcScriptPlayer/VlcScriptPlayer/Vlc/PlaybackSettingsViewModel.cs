using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

   [property: JsonIgnore]
   [ObservableProperty]
   private List<string> _monitors;
   partial void OnMonitorsChanged( List<string> value ) => _selectedMonitorIdx = Math.Min( _selectedMonitorIdx, value != null ? value.Count - 1 : 0 );

   [ObservableProperty]
   private int _selectedMonitorIdx;

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