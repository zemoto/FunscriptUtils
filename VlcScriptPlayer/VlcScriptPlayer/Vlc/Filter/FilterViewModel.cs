using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace VlcScriptPlayer.Vlc.Filter;

internal enum EqualizerPresets : uint
{
   Flat,
   Classical,
   Club,
   Dance,
   [Description( "Full Bass" )]
   FullBass,
   [Description( "Full Bass and Treble" )]
   FullBassAndTreble,
   [Description( "Full Treble" )]
   FullTreble,
   Headphones,
   [Description( "Large Hall" )]
   LargeHall,
   Live,
   Party,
   Pop,
   Raggae,
   Rock,
   Ska,
   Soft,
   [Description( "Soft Rock" )]
   SoftRock,
   Techno
}

internal sealed class FilterViewModel : ObservableObject
{
   private EqualizerPresets _equalizerPreset = EqualizerPresets.Headphones;
   public EqualizerPresets EqualizerPreset
   {
      get => _equalizerPreset;
      set => SetProperty( ref _equalizerPreset, value );
   }

   private bool _boostBass;
   public bool BoostBass
   {
      get => _boostBass;
      set => SetProperty( ref _boostBass, value );
   }

   private bool _boostSaturation;
   public bool BoostSaturation
   {
      get => _boostSaturation;
      set => SetProperty( ref _boostSaturation, value );
   }
}
