using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace VlcScriptPlayer.Vlc;

internal sealed class PlaybackSettingsViewModel : ObservableObject
{
   private List<string> _audioOutputs;
   [JsonIgnore]
   public List<string> AudioOutputs
   {
      get => _audioOutputs;
      set
      {
         if ( SetProperty( ref _audioOutputs, value ) && value?.Contains( _selectedAudioOutput ) == false )
         {
            SelectedAudioOutput = value.FirstOrDefault();
         }
      }
   }

   private string _selectedAudioOutput;
   public string SelectedAudioOutput
   {
      get => _selectedAudioOutput;
      set => SetProperty( ref _selectedAudioOutput, value );
   }

   private uint _cacheSize = 3000;
   public uint CacheSize
   {
      get => _cacheSize;
      set => SetProperty( ref _cacheSize, value );
   }

   private bool _UseHardwareDecoding = true;
   public bool UseHardwareDecoding
   {
      get => _UseHardwareDecoding;
      set => SetProperty( ref _UseHardwareDecoding, value );
   }

   private bool _loop;
   public bool Loop
   {
      get => _loop;
      set => SetProperty( ref _loop, value );
   }

   private bool _autoplay;
   public bool Autoplay
   {
      get => _autoplay;
      set => SetProperty( ref _autoplay, value );
   }

   [JsonIgnore]
   public RelayCommand ShowAdvancedPlaybackSettingsCommand { get; set; }
}