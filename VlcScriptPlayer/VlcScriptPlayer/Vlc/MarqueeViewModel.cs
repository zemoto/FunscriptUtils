using ZemotoCommon.UI;

namespace VlcScriptPlayer.Vlc;

internal enum MarqueePosition
{
   Info,
   Volume,
   Priority
}

internal enum MarqueeType
{
   General,
   Volume,
   Process
}

internal sealed class MarqueeViewModel : ViewModelBase
{
   private bool _enabled;
   private MarqueeType _type;
   private bool _displayingPriorityText;

   public void SetEnabled( bool enabled )
   {
      _enabled = enabled;
      if ( !_enabled )
      {
         _type = MarqueeType.General;
         _displayingPriorityText = false;
         Text = string.Empty;
      }
   }

   public void SetText( string text, MarqueeType type = MarqueeType.General )
   {
      if ( _enabled && !_displayingPriorityText )
      {
         _type = type;
         Text = text;
      }
   }

   public void SetPriorityText( string text )
   {
      _displayingPriorityText = true;
      Text = text;
   }

   public void FinalizePriorityText( string text )
   {
      if ( !_displayingPriorityText )
      {
         return;
      }

      _displayingPriorityText = false;
      SetText( text );
   }

   private string _text;
   public string Text
   {
      get => _text;
      private set => SetProperty( ref _text, value );
   }

   public MarqueePosition Position
   {
      get
      {
         if ( _displayingPriorityText )
         {
            return MarqueePosition.Priority;
         }

         return _type switch
         {
            MarqueeType.General => MarqueePosition.Info,
            MarqueeType.Volume => MarqueePosition.Volume,
            MarqueeType.Process => MarqueePosition.Info,
            _ => MarqueePosition.Info,
         };
      }
   }

   public bool IsPerpetual => _type is MarqueeType.Process || _displayingPriorityText;
}