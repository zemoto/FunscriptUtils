using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc.Filter;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI;

internal sealed class MainWindowViewModel : ViewModelBase
{
   public MainWindowViewModel() // Design-time
   {
      HandyVm = new HandyViewModel();
      FilterVm = new FilterViewModel();
      ScriptVm = new ScriptViewModel();
   }

   public MainWindowViewModel( HandyViewModel handyVm, FilterViewModel filterVm, ScriptViewModel scriptVm )
   {
      HandyVm = handyVm;
      FilterVm = filterVm;
      ScriptVm = scriptVm;
   }

   public HandyViewModel HandyVm { get; }
   public FilterViewModel FilterVm { get; }
   public ScriptViewModel ScriptVm { get; }

   public RelayCommand<bool> UploadScriptAndLaunchPlayerCommand { get; set; }
}
