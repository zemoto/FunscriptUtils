using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Windows;

namespace VlcScriptPlayer.Vlc;

internal sealed class VlcManager : IDisposable
{
   private readonly LibVLC _libvlc = new();
   private readonly VlcMarquee _marquee;
   public VlcFilter Filter { get; }
   public MediaPlayer Player { get; }
   public VlcTimeProvider TimeProvider { get; }

   public event EventHandler MediaSetupComplete;

   public VlcManager()
   {
      Player = new MediaPlayer( _libvlc )
      {
         FileCaching = 3000,
         EnableHardwareDecoding = true
      };

      _marquee = new VlcMarquee( Player );
      Filter = new VlcFilter( Player, _marquee );
      TimeProvider = new VlcTimeProvider( Player );

      Player.Paused += OnPlayerPaused;
   }

   public void Dispose()
   {
      _libvlc.Dispose();
      Filter.Dispose();
      Player.Dispose();
   }

   public void OpenVideo( string filePath, IFilterConfig filterConfig )
   {
      Filter.SetFilters( filterConfig );

      Player.Buffering += OnPlayerFirstTimeBuffering;
      var media = new Media( _libvlc, new Uri( filePath ) );
      media.Parse();
      Player.Play( media );
   }

   public void CloseVideo()
   {
      Player.Buffering -= OnPlayerFirstTimeBuffering;
      Filter.UnsetFilters();
      Player.Stop();
      Player.Media?.Dispose();
      Player.Media = null;
      _marquee.Enabled = false;
   }

   private void OnPlayerFirstTimeBuffering( object sender, MediaPlayerBufferingEventArgs e )
   {
      if ( e.Cache < 100 )
      {
         return;
      }

      _ = ThreadPool.QueueUserWorkItem( _ =>
      {
         Player.Buffering -= OnPlayerFirstTimeBuffering;
         Player.SetPause( true );
         Player.Time = 0;
         TimeProvider.Duration = TimeSpan.FromMilliseconds( Player.Media.Duration );

         Thread.Sleep( 500 ); // Give VLC time to process
         Application.Current.Dispatcher.BeginInvoke( () => MediaSetupComplete?.Invoke( this, EventArgs.Empty ) );
         _marquee.Enabled = true;
      } );
   }

   // Force the player to resync itself with where it paused, as this can be incorrect sometimes
   private void OnPlayerPaused( object sender, EventArgs e ) => Player.Position -= float.Epsilon;
}
