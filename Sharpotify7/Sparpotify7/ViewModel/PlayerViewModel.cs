using System;
using System.Windows.Controls;
using System.Windows.Media;
using Sharpotify.Media;
using Sparpotify7.Player;

namespace Sparpotify7.ViewModel
{
    public class PlayerViewModel : ViewModelBase
    {
        private Action<MediaStreamSource> playcallback;
        private MusicStream currentStream;

        public string Title { get { return Facade.CurrentTrack.Title; } }
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerViewModel"/> class.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="playcallback">The playcallback.</param>
        public PlayerViewModel(Page view, Action<MediaStreamSource> playcallback)
            : base(view)
        {
            this.playcallback = playcallback;
            this.Load();
        }

        private void Load()
        {
            //currentStream = Facade.GetMusicStream(Facade.CurrentTrack);
            //currentStream.NewDataAvailable += new EventHandler<EventArgs>(OnCurrentStreamNewDataAvailable);
            //currentStream.AllDataAvailable += new EventHandler<EventArgs>(currentStream_AllDataAvailable);
        }

        private void currentStream_AllDataAvailable(object sender, EventArgs e)
        {
             OggMediaStreamSource ogg = new OggMediaStreamSource(currentStream);
             playcallback(ogg);
           
        }

        private void OnCurrentStreamNewDataAvailable(object sender, EventArgs e)
        {
            int buffered = (int)(((double)currentStream.AvailableLength / (double)currentStream.Length) * 100);
            buffered = (buffered > 20) ? 20 : buffered;
            if (buffered >= 20)
            {
                currentStream.NewDataAvailable -= OnCurrentStreamNewDataAvailable;

                OggMediaStreamSource ogg = new OggMediaStreamSource(currentStream);
                playcallback(ogg);
            }
        }

    }
}
