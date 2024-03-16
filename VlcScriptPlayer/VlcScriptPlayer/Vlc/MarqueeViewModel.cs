using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal enum MarqueeType
{
   General,
   Volume,
   Process
}

internal sealed class MarqueeViewModel : ViewModelBase
{
   public void SetText( string text, MarqueeType type = MarqueeType.General )
   {
      if ( Enabled )
      {
         Text = text;
         Type = type;
      }
   }

   private bool _enabled;
   public bool Enabled
   {
      get => _enabled;
      set
      {
         if ( SetProperty( ref _enabled, value ) && !value )
         {
            Text = string.Empty;
         }
      }
   }

   private string _Text;
   public string Text
   {
      get => _Text;
      private set => SetProperty( ref _Text, value );
   }


   private MarqueeType _type;
   public MarqueeType Type
   {
      get => _type;
      private set => SetProperty( ref _type, value );
   }
}