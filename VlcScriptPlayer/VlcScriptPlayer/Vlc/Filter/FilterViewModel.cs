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

internal sealed partial class FilterViewModel : ObservableObject
{
   [ObservableProperty]
   private EqualizerPresets _equalizerPreset = EqualizerPresets.Headphones;

   [ObservableProperty]
   private bool _boostBass;

   [ObservableProperty]
   private bool _boostSaturation;
}
