using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Sharpotify;
using Sharpotify.Enums;
using Sharpotify.Media;

namespace Sparpotify7
{
    public class Facade
    {
        #region Fields
        private static ISpotify connection;
        private static User userInfo;
        private static PlaylistContainer container;
        private static List<Playlist> playlists;
        private static List<Track> topList;
        private static Track playingTrack;
        private static Dispatcher dispatcher;
        #endregion

        #region properties
        public static List<Playlist> Playlists { get { return playlists; } }
        public static List<Track> TopList { get { return topList; } }
        public static Track CurrentTrack { get; set; }
        #endregion

        #region Methods
        public static void Init(Dispatcher dispatcher)
        {
            connection = SpotifyPool.Instance;
            playlists = new List<Playlist>();
            Facade.dispatcher = dispatcher;
        }

        public static void Login(string user, string password, Action<bool> callback)
        {
            BackgroundWorker bw = new BackgroundWorker();
            var loggedIn = false;

            bw.DoWork += (sender, args) => {
                try
                {
                    try
                    {
                        connection.Login(user, password);
                    }
                    catch (Sharpotify.Exceptions.AuthenticationException ex)
                    {
                        throw new Exception("Unable to login: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Unspected Exception: " + ex.Message);
                    }

                    userInfo = connection.User();

                    container = connection.PlaylistContainer();

                    topList = connection.Toplist(ToplistType.Track, "", "").Tracks;

                    loggedIn = true;
                }
                catch (Exception ex)
                {
                    dispatcher.BeginInvoke(() => { MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error loging in...", MessageBoxButton.OK); });
                    connection.Close();
                }
            };

            bw.ProgressChanged += (sender, args) => {

            };

            bw.RunWorkerCompleted += (sender, args) => {
                callback(loggedIn);
            };

            bw.RunWorkerAsync();
        }

        public static void ReloadPlaylists(Action<bool, Playlist> callback)
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += (s, a) => 
            {
                if (container == null)
                {
                    container = connection.PlaylistContainer();
                }
                foreach (Playlist pl in container.Playlists)
                {
                    try
                    {
                        Playlist newPL = connection.Playlist(pl.Id);
                        playlists.Add(newPL);
                        callback(false, newPL);
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                }
                callback(true, null);
            };
            
            bw.RunWorkerAsync();
            
        }

        public static List<Track> GetTracks(Playlist playlist)
        {
            return connection.BrowseTracks(playlist.Tracks.Select(t => t.Id).ToList());
        }

        public static MusicStream GetMusicStream(Track track)
        {
            var file = track.Files[0];
            return connection.GetMusicStream(track, file, new TimeSpan(0, 0, 60));
        }

        public static Stream GetImage(string id)
        {
            return connection.ImageStream(id);
        }
        #endregion
    }
}
