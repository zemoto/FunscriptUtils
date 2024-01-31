using System;
using System.Windows.Input;
using VlcScriptPlayer.Handy;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal sealed class HotkeyManager : IDisposable
{
   private readonly VlcManager _vlc;
   private readonly HandyManager _handy;

   public HotkeyManager( VlcManager vlc, HandyManager handy )
   {
      _vlc = vlc;
      _handy = handy;

      _vlc.MediaOpened += OnMediaOpened;
      _vlc.MediaClosing += OnMediaClosing;
   }

   public void Dispose()
   {
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
      _vlc.MediaOpened -= OnMediaOpened;
      _vlc.MediaClosing -= OnMediaClosing;
   }

   private void OnMediaOpened( object sender, EventArgs e )
   {
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
      InputManager.Current.PreProcessInput += OnInputManagerPreProcessInput;
   }

   private void OnMediaClosing( object sender, EventArgs e ) => InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;

   private async void OnInputManagerPreProcessInput( object sender, PreProcessInputEventArgs e )
   {
      if ( e?.StagingItem?.Input is not KeyEventArgs keyEvent || keyEvent.RoutedEvent != Keyboard.KeyDownEvent )
      {
         return;
      }

      switch ( keyEvent.Key )
      {
         case Key.Space:
            _vlc.TogglePlayPause();
            break;
         case Key.Escape:
            _vlc.CloseVideo();
            break;
         case Key.B when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
            _vlc.Filter.BassBoostEnabled = !_vlc.Filter.BassBoostEnabled;
            break;
         case Key.S when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
            _vlc.Filter.SaturationBoostEnabled = !_vlc.Filter.SaturationBoostEnabled;
            break;
         case Key.R when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
            var (handyMin, handyMax) = await _handy.GetHandyRangeAsync();
            _vlc.Marquee.DisplayText( $"{handyMin} - {handyMax}" );
            break;
      }
   }
}
