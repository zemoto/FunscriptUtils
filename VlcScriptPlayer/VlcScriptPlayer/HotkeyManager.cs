﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VlcScriptPlayer.UI.VideoPlayer;
using VlcScriptPlayer.Vlc;

namespace VlcScriptPlayer;

internal sealed class HotkeyManager : IDisposable
{
   private readonly VlcManager _vlc;

   public HotkeyManager( VlcManager vlc )
   {
      _vlc = vlc;
      _vlc.MediaOpened += OnMediaOpened;
      _vlc.MediaClosed += OnMediaClosed;
   }

   public void Dispose()
   {
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
      _vlc.MediaOpened -= OnMediaOpened;
      _vlc.MediaClosed -= OnMediaClosed;
   }

   private void OnMediaOpened( object sender, EventArgs e )
   {
      InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
      InputManager.Current.PreProcessInput += OnInputManagerPreProcessInput;
   }

   private void OnMediaClosed( object sender, EventArgs e ) => InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;

   private void OnInputManagerPreProcessInput( object sender, PreProcessInputEventArgs e )
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
            var videoPlayer = Application.Current.Windows.OfType<VideoPlayerWindow>().FirstOrDefault();
            videoPlayer?.Close();
            break;
         case Key.B when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
            _vlc.Filter.BassBoostEnabled = !_vlc.Filter.BassBoostEnabled;
            break;
         case Key.S when ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control:
            _vlc.Filter.SaturationBoostEnabled = !_vlc.Filter.SaturationBoostEnabled;
            break;
      }
   }
}
