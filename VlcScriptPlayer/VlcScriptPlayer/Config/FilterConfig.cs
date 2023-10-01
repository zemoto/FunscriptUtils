﻿using ZemotoCommon.UI;

namespace VlcScriptPlayer.Config;

internal sealed class FilterConfig : ViewModelBase
{
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