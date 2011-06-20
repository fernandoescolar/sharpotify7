using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Navigation;
using Sharpotify.Media;
using Sparpotify7.Commands;
using System.Windows.Input;

namespace Sparpotify7.ViewModel
{
    /// <summary>
    /// MainPanoramaViewModel
    /// </summary>
    public class MainPanoramaViewModel : ViewModelBase
    {
        private ItemViewModel<Playlist> selectedPlayList;
        private ItemViewModel<Track> selectedTopTrack;
        private string searchText;
        private bool isWorking = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPanoramaViewModel"/> class.
        /// </summary>
        /// <param name="view">The view.</param>
        public MainPanoramaViewModel(Page view)
            : base(view)
        {
            this.PlayListItems = new ObservableCollection<ItemViewModel<Playlist>>();
            this.TopTracktItems = new ObservableCollection<ItemViewModel<Track>>();
            this.LoadData();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is working.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is working; otherwise, <c>false</c>.
        /// </value>
        public bool IsWorking { get { return this.isWorking; } set { this.isWorking = value; OnPropertyChanged("IsWorking"); } }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel<Playlist>> PlayListItems { get; private set; }

        /// <summary>
        /// Gets the top trackt items.
        /// </summary>
        public ObservableCollection<ItemViewModel<Track>> TopTracktItems { get; private set; }

        /// <summary>
        /// Gets or sets the selected play list.
        /// </summary>
        /// <value>
        /// The selected play list.
        /// </value>
        public ItemViewModel<Playlist> SelectedPlayList
        {
            get { return this.selectedPlayList; }
            set 
            {
                this.selectedPlayList = value;
                OnPropertyChanged("SelectedPlayList");
                if (value != null)
                {
                    NavigationService.Navigate(new Uri("/View/PlaylistView.xaml?playlistid=" + value.Model.Id, UriKind.Relative));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected top track.
        /// </summary>
        /// <value>
        /// The selected top track.
        /// </value>
        public ItemViewModel<Track> SelectedTopTrack
        {
            get { return this.selectedTopTrack; }
            set
            {
                this.selectedTopTrack = value;
                OnPropertyChanged("SelectedTopTrack");
                if (value != null)
                {
                    Facade.CurrentTrack = value.Model;
                    NavigationService.Navigate(new Uri("/View/PlayerPage.xaml", UriKind.Relative));
                }
            }
        }

        /// <summary>
        /// Gets or sets the search text.
        /// </summary>
        /// <value>
        /// The search text.
        /// </value>
        public string SearchText
        {
            get { return this.searchText; }
            set
            {
                this.searchText = value;
                OnPropertyChanged("SearchText");
            }
        }

        /// <summary>
        /// Gets the search command.
        /// </summary>
        public ICommand SearchCommand
        {
            get { return new DelegateCommand(this.SearchCommandExecute, this.SearchCommandCanExecute); }
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        public void LoadData()
        {
            IsWorking = true;

            if (Facade.Playlists.Count > 0)
            {
                foreach (var playlist in Facade.Playlists)
                {
                    this.PlayListItems.Add(new ItemViewModel<Playlist>
                    { 
                         LineOne = playlist.Name,
                         LineTwo = "songs " + playlist.Tracks.Count,
                         Model = playlist
                    });
                }
            }

            Facade.ReloadPlaylists(LoadPlaylist);

            if (Facade.TopList.Count > 0)
            {
                foreach (var t in Facade.TopList)
                {
                    this.TopTracktItems.Add(new ItemViewModel<Track>
                    {
                        LineOne = t.Title,
                        LineTwo = t.Album.Name,
                        LineThree = t.Artist.Name,
                        Model = t
                    });
                }
            }
        }

        /// <summary>
        /// Asyc Loads the playlist callback.
        /// </summary>
        /// <param name="done">if set to <c>true</c> [done].</param>
        /// <param name="playlist">The playlist.</param>
        private void LoadPlaylist(bool done, Playlist playlist)
        {
            if (done)
            {
                var knownids = Facade.Playlists.Select(p => p.Id).ToList();
                List<ItemViewModel<Playlist>> toRemove = new List<ItemViewModel<Playlist>>();

                for (int i = 0; i < this.PlayListItems.Count; i++)
                {
                    var aux = this.PlayListItems[i];
                    if (knownids != null && !knownids.Contains(aux.Model.Id))
                    {
                        toRemove.Add(aux);
                    }
                }

                foreach (var idToRemove in toRemove)
                {
                    this.PlayListItems.Remove(idToRemove);
                }
                OnPropertyChanged("PlayListItems");
                IsWorking = false;
            }
            else if (playlist != null)
            {
                ItemViewModel<Playlist> item = this.PlayListItems.Where(i => i.Model.Id == playlist.Id).SingleOrDefault();
                bool add = false;
                if (item == null)
                {
                    item = new ItemViewModel<Playlist>();
                    add = true;
                }

                item.LineOne = playlist.Name;
                item.LineTwo = "songs " + playlist.Tracks.Count;
                item.Model = playlist;

                if (add)
                {
                   this.CurrentDispatcher.BeginInvoke(() => this.PlayListItems.Add(item));
                }
                else
                {
                    OnPropertyChanged("PlayListItems");
                }
            }
        }

        /// <summary>
        /// Searches the command can execute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        private bool SearchCommandCanExecute(object obj)
        {
            return !string.IsNullOrEmpty(this.SearchText);
        }

        /// <summary>
        /// Searches the command execute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        private void SearchCommandExecute(object obj)
        { 
        
        }
    }
}
