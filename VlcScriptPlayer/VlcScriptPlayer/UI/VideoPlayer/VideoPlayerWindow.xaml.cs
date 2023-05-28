using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.UI.VideoPlayer;

internal partial class VideoPlayerWindow
{
	private readonly DispatcherTimer _hideScrubberTimer;
	private readonly VlcManager _vlc;

	private DateTime _lastPauseToggleTime = DateTime.MinValue;

	public VideoPlayerWindow( VlcManager vlc )
	{
		_vlc = vlc;
		_hideScrubberTimer = new DispatcherTimer( TimeSpan.FromSeconds( 3 ), DispatcherPriority.Normal, OnHideScrubberTimerTick, Dispatcher ) { IsEnabled = false };

		InitializeComponent();

		VideoPlayer.Margin = new Thickness( SystemParameters.WindowResizeBorderThickness.Left * 2 - 1 );
		VideoPlayer.MediaPlayer = _vlc.Player;

		UniversalClick.AddClickHandler( VideoClickHandler, OnVideoClick );
		InputManager.Current.PreProcessInput += OnInputManagerPreProcessInput;
		_vlc.MediaSetupComplete += OnMediaSetupComplete;
	}

	private void OnMediaSetupComplete( object sender, EventArgs e )
	{
		PlayPauseindicator.Player = VideoPlayer.MediaPlayer;
		VideoControls.Player = VideoPlayer.MediaPlayer;
	}

	private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
	{
		Mouse.OverrideCursor = null;
		_hideScrubberTimer.Stop();
		InputManager.Current.PreProcessInput -= OnInputManagerPreProcessInput;
		_vlc.MediaSetupComplete -= OnMediaSetupComplete;
	}

	private void OnInputManagerPreProcessInput( object sender, PreProcessInputEventArgs e )
	{
		if ( e?.StagingItem?.Input is not KeyEventArgs keyEvent || keyEvent.RoutedEvent != Keyboard.KeyDownEvent )
		{
			return;
		}

		switch ( keyEvent.Key )
		{
			case Key.Space:
			{
				ThrottledTogglePause();
				break;
			}
			case Key.Escape:
			{
				Close();
				break;
			}
		}
	}

	private void OnMouseMoveOverVideo( object sender, MouseEventArgs e )
	{
		Mouse.OverrideCursor = null;
		VideoControls.Visibility = Visibility.Visible;
		_hideScrubberTimer.Stop();
		_hideScrubberTimer.Start();
	}

	private void OnHideScrubberTimerTick( object sender, EventArgs e )
	{
		_hideScrubberTimer.Stop();
		if ( !VideoControls.IsMouseOver )
		{
			VideoControls.Visibility = Visibility.Collapsed;
			Mouse.OverrideCursor = Cursors.None;
		}
	}

	private void OnMouseLeavingVideo( object sender, MouseEventArgs e )
	{
		Mouse.OverrideCursor = null;
		_hideScrubberTimer.Stop();
		VideoControls.Visibility = Visibility.Collapsed;
	}

	private void OnMouseWheel( object sender, MouseWheelEventArgs e )
	{
	   const int volumeIncrement = 5;

		var player = VideoPlayer.MediaPlayer;
		var volume = player.Volume;
		if ( e.Delta > 0 )
		{
			if ( volume == 100 )
			{
				return;
			}

			volume = Math.Min( volume + volumeIncrement, 100 );
			player.Volume = volume;
		}
		else if ( e.Delta < 0 )
		{
			if ( volume == 0 )
			{
				return;
			}

			volume = Math.Max( volume - volumeIncrement, 0 );
			player.Volume = volume;
		}
	}

	private void OnVideoClick( object sender, RoutedEventArgs e ) => ThrottledTogglePause();

	private void ThrottledTogglePause()
	{
		if ( DateTime.Now < _lastPauseToggleTime + TimeSpan.FromSeconds( 1 ) )
		{
			return;
		}

		var player = VideoPlayer.MediaPlayer;
		if ( player.CanPause )
		{
			player.Pause();
		}
		else
		{
			player.Play();
		}

		_lastPauseToggleTime = DateTime.Now;
	}
}
