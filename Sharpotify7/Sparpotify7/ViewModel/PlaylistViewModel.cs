using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Sharpotify.Media;

namespace Sparpotify7.ViewModel
{
    public class PlaylistViewModel : ViewModelBase
    {
        private Playlist playlist;

        private ItemViewModel<Track> selectedTrack;

        private bool isWorking = false;

        private int imageLoadingCounter = 0;

        public bool IsWorking
        {
            get { return isWorking; }
            
            set 
            { 
                isWorking = value;
                OnPropertyChanged("IsWorking");
            }
        }
        /// <summary>
        /// Gets the title.
        /// </summary>
        public string Title { get { return playlist.Name; } }

        /// <summary>
        /// Gets the play list items.
        /// </summary>
        public ObservableCollection<ItemViewModel<Track>> TrackItems { get; private set; }

        /// <summary>
        /// Gets or sets the selected play list.
        /// </summary>
        /// <value>
        /// The selected play list.
        /// </value>
        public ItemViewModel<Track> SelectedTrack
        {
            get { return this.selectedTrack; }
            set
            {
                this.selectedTrack = value;
                OnPropertyChanged("SelectedPlayList");
                if (value != null)
                {
                    Facade.CurrentTrack = value.Model;
                    NavigationService.Navigate(new Uri("/View/PlayerPage.xaml", UriKind.Relative));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistViewModel"/> class.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="id">The id.</param>
        public PlaylistViewModel(Page view, string id) : base(view)
        {
            this.TrackItems = new ObservableCollection<ItemViewModel<Track>>();
            
            foreach (var pl in Facade.Playlists)
            {
                if (pl.Id == id)
                {
                    this.playlist = pl;
                    break;
                }
            }

            this.LoadData();
        }

        private void LoadData()
        {
            var tracks = Facade.GetTracks(this.playlist);
            foreach (var track in tracks)
            {
                var item = new ItemViewModel<Track>()
                {
                    LineOne = track.Title,
                    LineTwo = track.Artist.Name,
                    LineThree = track.Album.Name,
                    Model = track
                };
                
                this.TrackItems.Add(item);

                LoadImage(item);
            }
        }

        private void LoadImage(ItemViewModel<Track> item)
        {
            if (item.Model.Cover == null) return;
            if (imageLoadingCounter == 0) IsWorking = true;

            imageLoadingCounter++;
            ThreadPool.QueueUserWorkItem((state) =>
                                             {
                                                 var imageStream = Facade.GetImage(item.Model.Cover);
                                                 CurrentDispatcher.BeginInvoke(() =>
                                                    {
                                                        var image = new BitmapImage();
                                                        image.SetSource(imageStream);
                                                        item.Image = image;
                                                        imageLoadingCounter--;
                                                        
                                                        if (imageLoadingCounter == 0) IsWorking = false;
                                                    });
                                                 
                                             });
        }
    }
}
