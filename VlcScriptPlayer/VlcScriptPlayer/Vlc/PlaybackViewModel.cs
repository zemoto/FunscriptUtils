using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal sealed class PlaybackViewModel : ViewModelBase
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
}