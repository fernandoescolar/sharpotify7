using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sharpotify.Media;
using Path = System.IO.Path;

namespace Sparpotify7.Player
{
    public class OggMediaPlayer : UserControl
    {
        private MusicStream currentStream;

        private MediaElement mediaElement;

        private int currentTrackIndex;

        private List<Track> tracks;

        public IList<Track> Tracks { get { return this.tracks; } }

        public int CurrentTrackIndex { get { return this.currentTrackIndex; } }

        public OggMediaPlayer()
        {
            this.mediaElement = new MediaElement();
            this.Content = this.mediaElement;
            this.tracks = new List<Track>();
            this.currentTrackIndex = 0;
            this.InitializeMediaElement(this.mediaElement);
        }

         public void SetTracks(List<Track> tracks)
         {
             this.SetTracks(tracks, 0);
         }

        public void SetTracks(List<Track> tracks, int index)
        {
            this.mediaElement.Stop();
            this.tracks = tracks;
            this.currentTrackIndex = index;
            this.Play();
        }

        public void Play()
        {
            if (this.mediaElement.CurrentState == MediaElementState.Playing)
            {
                return;
            }

            if (this.mediaElement.CurrentState == MediaElementState.Paused)
            {
                this.mediaElement.Play();    
            }

            if (this.mediaElement.CurrentState == MediaElementState.Stopped)
            {
                if (this.CurrentTrackIndex >= Tracks.Count)
                {
                    this.currentTrackIndex = 0;
                }

                if (this.CurrentTrackIndex < 0)
                {
                    this.currentTrackIndex = this.Tracks.Count - 1;
                }

                this.GetSong();
            }
        }

        public void Pause()
        {
            this.mediaElement.Pause();
        }

        public void Stop()
        {
            this.mediaElement.Stop();
        }

        public void Next()
        {
            ReleaseCurrentStream();
            this.currentTrackIndex++;
            this.Play();
        }

        public void Previous()
        {
            ReleaseCurrentStream();
            this.currentTrackIndex--;
            this.Play();
        }

        protected void InitializeMediaElement(MediaElement me)
        {
            me.AutoPlay = false;
            me.CurrentStateChanged += new RoutedEventHandler(OnMediaElementStateChanged);
            me.MediaOpened += new RoutedEventHandler(OnMediaElementMediaOpened);
            me.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(OnMediaElementMediaFailed);
            me.MediaEnded += new RoutedEventHandler(OnMediaElementMediaEnded);
        }

        protected void GetSong()
        {
            string id = this.Tracks[this.CurrentTrackIndex].Id;
            string path = Path.Combine("ogg", id);

            IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
            if (iso.FileExists(path))
            {
                using(Stream s = iso.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    MemoryStream m = new MemoryStream();
                    
                    byte[] buffer = new byte[4096];
                    while(s.CanRead)
                    {
                        int read = s.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            m.Write(buffer, 0, read);
                        }
                    }
                    
                    this.mediaElement.SetSource(new OggMediaStreamSource(m));
                    this.mediaElement.Play();
                }
            }
            else
            {
                currentStream = Facade.GetMusicStream(Facade.CurrentTrack);
                currentStream.NewDataAvailable += new EventHandler<EventArgs>(OnCurrentStreamNewDataAvailable);
                currentStream.AllDataAvailable += new EventHandler<EventArgs>(currentStream_AllDataAvailable);  
            }
        }

        private void OnMediaElementMediaEnded(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void OnMediaElementMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
        }

        private void OnMediaElementMediaOpened(object sender, RoutedEventArgs e)
        {
        }

        private void OnMediaElementStateChanged(object sender, RoutedEventArgs e)
        {
            switch(this.mediaElement.CurrentState)
            {
                case MediaElementState.Buffering:
                    break;
                case MediaElementState.Closed:
                    break;
                case MediaElementState.Opening:
                    break;
                case MediaElementState.Paused:
                    break;
                case MediaElementState.Playing:
                    break;
                case MediaElementState.Stopped:
                    break;
            }
        }

        private void ReleaseCurrentStream()
        {
            if (currentStream != null)
            {
                currentStream.AllDataAvailable -= currentStream_AllDataAvailable;
                currentStream.NewDataAvailable -= OnCurrentStreamNewDataAvailable;
                currentStream = null;
            }
        }

        private void currentStream_AllDataAvailable(object sender, EventArgs e)
        {
            ReleaseCurrentStream();
        }

        private void OnCurrentStreamNewDataAvailable(object sender, EventArgs e)
        {
            int buffered = (int)(((double)currentStream.AvailableLength / (double)currentStream.Length) * 100);
            buffered = (buffered > 20) ? 20 : buffered;
            if (buffered >= 20)
            {
                currentStream.NewDataAvailable -= OnCurrentStreamNewDataAvailable;

                OggMediaStreamSource ogg = new OggMediaStreamSource(currentStream);
                Dispatcher.BeginInvoke(() =>
                                           {
                                               this.mediaElement.SetSource(ogg);
                                               this.mediaElement.Play();                               
                                           });
            }
        }
    }
}
