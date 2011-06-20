﻿namespace Sharpotify
{
    using Sharpotify.Cache;
    using Sharpotify.Crypto;
    using Sharpotify.Enums;
    using Sharpotify.Exceptions;
    using Sharpotify.Media;
    using Sharpotify.Media.Parser;
    using Sharpotify.Protocol;
    using Sharpotify.Protocol.Channel;
    using Sharpotify.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Windows.Media.Imaging;

    public class SpotifyConnection : ICommandHandler, ISpotify
    {
        #region Fields
        /*
	     * Session and protocol associated with this connection.
	     */
        protected Session session;
        protected Sharpotify.Protocol.Protocol protocol;
        /*
	     * User information.
	     */
        private User user;
        private Semaphore userSemaphore;

        /*
         * cache.
         */
        private ICache cache;

        /*
         * Status and timeout.
         */
        private bool running;
        private TimeSpan timeout;
        #endregion
        #region Properties
        public TimeSpan Timeout { get { return timeout; } set { timeout = value; } }

        #endregion
        #region Factory
        public SpotifyConnection(int clientOs, int clientRevision) : this(clientOs, clientRevision, new IsolatedStorageCache(), new TimeSpan(0, 0, 10))
        {
        }

        public SpotifyConnection(int clientOs, int clientRevision, ICache cache, TimeSpan timeout)
        {
            this.session = new Session(clientOs, clientRevision);
            this.protocol = null;
            this.running = false;
            this.user = null;
            this.userSemaphore = new Semaphore(0, 2);
            this.cache = cache;
            this.timeout = timeout;
        }
        public SpotifyConnection() : this(new IsolatedStorageCache(), new TimeSpan(0, 0, 10))
        {
        }

        public SpotifyConnection(ICache cache, TimeSpan timeout)
        {
            this.session = new Session();
            this.protocol = null;
            this.running = false;
            this.user = null;
            this.userSemaphore = new Semaphore(0, 2);
            this.cache = cache;
            this.timeout = timeout;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Continuously receives packets in order to handle them.
        /// </summary>
        public void Run()
        {
            /* Fail quietly. */
            if (this.running)
            {
                return;
            }
            /* Check if we're logged in. */
            if (this.protocol == null)
            {
                throw new InvalidOperationException("You need to login first!");
            }

            this.running = true;

            /* Continuously receive packets until connection is closed. */
            try
            {
                //while (true)
                {
                    if (this.protocol == null)
                    {
                        return;
                    }

                    this.protocol.ReceivePacket();
                }
            }
            catch (ProtocolException e)
            {
                /* Connection was closed. */
            }
            finally
            {
                this.running = false;
            }
        }
        #endregion
        #region ISpotify Methods
        /// <summary>
        /// Login to Spotify using the specified username and password.
        /// </summary>
        /// <param name="username">Username to use.</param>
        /// <param name="password">Corresponding password.</param>
        public void Login(string username, string password)
        {
            if (this.protocol != null)
            {
                throw new InvalidOperationException("Already logged in!");
            }
            this.protocol = this.session.Authenticate(username, password);
            this.user = new Sharpotify.Media.User(username);
            this.protocol.AddHandler(this);

            /*Start the thread*/
            //System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(delegate(object objState) { this.Run(); }));
            System.Threading.Thread backgroundProcess = new System.Threading.Thread(delegate() { this.Run(); });
            backgroundProcess.Name = "Sharpotify.Background";
            backgroundProcess.IsBackground = true;
            //backgroundProcess.Priority = System.Threading.ThreadPriority.BelowNormal;
            backgroundProcess.Start();
            //this.Run();
            //System.Threading.Tasks.Task.Factory.StartNew(() => Run());
        }
        /// <summary>
        /// Closes the connection to a Spotify server.
        /// </summary>
        public void Close()
        {
            if (this.protocol != null)
            {
                this.protocol.Disconnect();
                this.protocol = null;
            }
            if (this.userSemaphore is IDisposable)
            {
                (this.userSemaphore as IDisposable).Dispose();
            }
        }
        /// <summary>
        /// Get user info.
        /// </summary>
        /// <returns>A <see cref="User"/> object.</returns>
        public User User()
        {
            try
            {
                /* Wait for data to become available (country, prodinfo). */
                if (!this.userSemaphore.TryAcquire(2, this.timeout))
                {
                    throw new TimeoutException("Timeout while waiting for user data.");
                }
            }
            catch (TimeoutException exception)
            {
                throw new Exception("Timeout", exception);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                /* Release so this can be called again. */
                this.userSemaphore.Release(2);
            }
            return this.user;
        }
        /// <summary>
        /// Fetch a toplist.
        /// </summary>
        /// <param name="type">A toplist type. e.g. "artist", "album" or "track".</param>
        /// <param name="region">A region code or null. e.g. "SE" or "DE".</param>
        /// <param name="username">A username or null.</param>
        /// <returns></returns>
        public Result Toplist(ToplistType type, string region, string username)
        {
            /* Create channel callback and parameter map. */
            ChannelCallback handler = new ChannelCallback();
            Dictionary<string, string> paramsarg = new Dictionary<string, string>();
            string str = EnumUtils.GetName(typeof(ToplistType), type).ToLower();

            /* Add parameters. */
            paramsarg.Add("type", str);
            if (!string.IsNullOrEmpty(region))
            {
                 paramsarg.Add("region", region);
            }
            if (!string.IsNullOrEmpty(username))
            {
                paramsarg.Add("username", username);
            }

            /* Send toplist request. */
            try
            {
                this.protocol.SendToplistRequest(handler, paramsarg);
            }
            catch (ProtocolException)
            {
                return null;
            }
            /* Get data. */
            byte[] data = handler.Get(this.timeout);

            /* Create result from XML. */
            return XmlMediaParser.ParseResult(data);
        }
        /// <summary>
        /// Search for an artist, album or track.
        /// </summary>
        /// <param name="query">Your search query.</param>
        /// <returns>A <see cref="Result"/> object.</returns>
        public Result Search(string query)
        {
            ChannelCallback handler = new ChannelCallback();
            try
            {
                this.protocol.SendSearchQuery(handler, query);
            }
            catch (ProtocolException)
            {
                return null;
            }
            Result result = (Result)XmlMediaParser.Parse(handler.Get(this.Timeout));
            result.Query = query;
            return result;
        }
        /// <summary>
        /// Get an image (e.g. artist portrait or cover) by requesting
        /// it from the server or loading it from the local cache, if
        /// available.
        /// </summary>
        /// <param name="id">Id of the image to get.</param>
        /// <returns>An <see cref="System.Drawing.Image"/> or null if the request failed.</returns>
        public BitmapImage Image(string id)
        {
            byte[] buffer;
            if ((this.cache != null) && this.cache.Contains("image", id))
            {
                buffer = this.cache.Load("image", id);
            }
            else
            {
                ChannelCallback handler = new ChannelCallback();
                try
                {
                    this.protocol.SendImageRequest(handler, id);
                }
                catch (ProtocolException)
                {
                    return null;
                }
                buffer = handler.Get(this.Timeout);
                if (this.cache != null)
                {
                    this.cache.Store("image", id, buffer);
                }
            }
            try
            {
                var img = new BitmapImage();
                img.SetSource(new MemoryStream(buffer));
                return img;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// Images the stream.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public Stream ImageStream(string id)
        {
            byte[] buffer = null;
            if ((this.cache != null) && this.cache.Contains("image", id))
            {
                buffer = this.cache.Load("image", id);
            }
            else
            {
                ChannelCallback handler = new ChannelCallback();
                try
                {
                    this.protocol.SendImageRequest(handler, id);
                }
                catch (ProtocolException)
                {
                    return null;
                }
                buffer = handler.Get(this.Timeout);
                if (this.cache != null)
                {
                    this.cache.Store("image", id, buffer);
                }
            }

            return new MemoryStream(buffer);
        }
        /// <summary>
        /// Browse artist, album or track info.
        /// </summary>
        /// <param name="type">Type of media to browse for.</param>
        /// <param name="id">A 32-character hex string or a Spotify URI.</param>
        /// <returns></returns>
        private object Browse(BrowseType type, string id)
        {
            if (id.Length != 32 && !Hex.IsHex(id))
            {
                try
                {
                    Link link = Link.Create(id);

                    if ((type == BrowseType.Artist && !link.IsArtistLink) ||
                       (type == BrowseType.Album && !link.IsAlbumLink) ||
                       (type == BrowseType.Track && !link.IsTrackLink))
                    {
                        throw new ArgumentException("Browse type doesn't match given Spotify URI.");
                    }

                    id = link.Id;
                }
                catch (InvalidSpotifyURIException e)
                {
                    throw new ArgumentException("Given id is neither a 32-character hex string nor a valid Spotify URI.");
                }
            }
            
            /* Create channel callback. */
            ChannelCallback handler = new ChannelCallback();

            /* Send browse request. */
            try
            {
                this.protocol.SendBrowseRequest(handler, (int)type, id);
            }
            catch (ProtocolException)
            {
                return null;
            }
            return XmlMediaParser.Parse(handler.Get(this.Timeout));
        }
        /// <summary>
        /// Browse artist info by id.
        /// </summary>
        /// <param name="id">A 32-character hex string or a Spotify URI.</param>
        /// <returns></returns>
        public Artist BrowseArtist(string id)
        {
            object artist = this.Browse(BrowseType.Artist, id);
            if (artist is Artist)
                return (artist as Artist);
            return null;
        }
        /// <summary>
        /// Browse artist info.
        /// </summary>
        /// <param name="artist">An <see cref="Artist"/> object identifying the artist to browse.</param>
        /// <returns></returns>
        public Artist Browse(Artist artist)
        {
            return this.BrowseArtist(artist.Id);
        }
        /// <summary>
        /// Browse album info by id.
        /// </summary>
        /// <param name="id">A 32-character hex string or a Spotify URI.</param>
        /// <returns></returns>
        public Album BrowseAlbum(string id)
        {
            object album = this.Browse(BrowseType.Album, id);
            if (album is Album)
                return (album as Album);
            return null;
        }
        /// <summary>
        /// Browse album info.
        /// </summary>
        /// <param name="album">An <see cref="Album"/> object identifying the album to browse.</param>
        /// <returns></returns>
        public Album Browse(Album album)
        {
            return this.BrowseAlbum(album.Id);
        }
        /// <summary>
        /// Browse track info by id.
        /// </summary>
        /// <param name="id">A 32-character hex string or a Spotify URI.</param>
        /// <returns></returns>
        public Track BrowseTrack(string id)
        {
            object track = this.Browse(BrowseType.Track, id);
            if (track is Result)
                return (track as Result).Tracks[0];
            if (track is Track)
                return (track as Track);
            return null;
        }
        /// <summary>
        /// Browse track info.
        /// </summary>
        /// <param name="track">A <see cref="Track"/> object identifying the track to browse.</param>
        /// <returns></returns>
        public Track Browse(Track track)
        {
            return this.BrowseTrack(track.Id);
        }
        /// <summary>
        /// Browse information for multiple tracks by id.
        /// </summary>
        /// <param name="ids">A List of ids identifying the tracks to browse.</param>
        /// <returns></returns>
        public List<Track> BrowseTracks(List<string> ids)
        {
            byte[] data;
            StringBuilder hashBuffer = new StringBuilder();

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];

                if (id.Length != 32 && !Hex.IsHex(id))
                {
                    try
                    {
                        Link link = Link.Create(id);

                        if (!link.IsTrackLink)
                        {
                            throw new ArgumentException("Browse type doesn't match given Spotify URI.");
                        }

                        id = link.Id;

                        /* Set parsed id in list. */
                        ids[i] = id;
                    }
                    catch (InvalidSpotifyURIException e)
                    {
                        throw new ArgumentException("Given id is neither a 32-character hex string nor a valid Spotify URI.");
                    }
                }
                hashBuffer.Append(id);
            }

            string hash = Hex.ToHex(Hash.Sha1(Hex.ToBytes(hashBuffer.ToString())));

            /* Check cache. */
            if ((this.cache != null) && this.cache.Contains("browse", hash))
            {
                data = this.cache.Load("browse", hash);
            }
            else
            {
                /* Create channel callback */
                ChannelCallback handler = new ChannelCallback();

                /* Send browse request. */
                try
                {
                    this.protocol.SendBrowseRequest(handler, 3, ids);
                }
                catch (ProtocolException)
                {
                    return null;
                }
                
                /* Get data. */
                data = handler.Get(this.Timeout);

                /* Save to cache. */
                if (this.cache != null)
                {
                    this.cache.Store("browse", hash, data);
                }
            }
            return XmlMediaParser.ParseResult(data).Tracks;
        }
        /// <summary>
        /// Browse information for multiple tracks.
        /// </summary>
        /// <param name="tracks">A List of <see cref="Track"/> objects identifying the tracks to browse.</param>
        /// <returns></returns>
        public List<Track> Browse(List<Track> tracks)
        {
            List<string> ids = new List<string>();
            foreach (Track track in tracks)
            {
                ids.Add(track.Id);
            }
            return this.BrowseTracks(ids);
        }
        /// <summary>
        /// Request a replacement track.
        /// </summary>
        /// <param name="track">The track to search the replacement for.</param>
        /// <returns></returns>
        public Track Replacement(Track track)
        {
            List<Track> list = new List<Track>();
            list.Add(track);
            return this.Replacement(list)[0];
	    }
        /// <summary>
        /// Request multiple replacement track.
        /// </summary>
        /// <param name="tracks">The tracks to search the replacements for.</param>
        /// <returns></returns>
        public List<Track> Replacement(List<Track> tracks)
        {
		    /* Create channel callback */
		    ChannelCallback callback = new ChannelCallback();

		    /* Send browse request. */
		    try{
			    this.protocol.SendReplacementRequest(callback, tracks);
		    }
		    catch(ProtocolException e){
			    return null;
		    }

		    /* Get data. */
		    byte[] data = callback.Get(this.timeout);

		    /* Create result from XML. */
		    return XmlMediaParser.ParseResult(data).Tracks;
	    }
        /// <summary>
        /// Get stored user playlists.
        /// </summary>
        /// <returns></returns>
        public PlaylistContainer PlaylistContainer() 
        {
		    /* Create channel callback. */
		    ChannelCallback callback = new ChannelCallback();

		    /* Send request and parse response. */
		    try
            {
			    this.protocol.SendPlaylistRequest(callback, null);

			    /* Create and return playlist. */
			    return XmlPlaylistParser.ParsePlaylistContainer(callback.Get(this.timeout));
		    }
		    catch(ProtocolException e)
            {
                return Sharpotify.Media.PlaylistContainer.Empty;
		    }
	    }
        /// <summary>
        /// Add a playlist to a playlist container.
        /// </summary>
        /// <param name="playlistContainer">A <see cref="Sharpotify.Media.PlaylistContainer"/> to add the playlist to.</param>
        /// <param name="playlist">The <see cref="Playlist"/> to be added.</param>
        /// <returns></returns>
        public bool PlaylistContainerAddPlaylist(PlaylistContainer playlistContainer, Playlist playlist) 
        {
		    return this.PlaylistContainerAddPlaylist(playlistContainer, playlist, playlistContainer.Playlists.Count);
	    }
        /// <summary>
        /// Add a playlist to a playlist container.
        /// </summary>
        /// <param name="playlistContainer">The playlist container.</param>
        /// <param name="playlist">The playlist to be added.</param>
        /// <param name="position">The target position of the added playlist.</param>
        /// <returns></returns>
        public bool PlaylistContainerAddPlaylist(PlaylistContainer playlistContainer, Playlist playlist, int position)
        {
            List<Playlist> playlists = new List<Playlist>();
		    playlists.Add(playlist);
		    return this.PlaylistContainerAddPlaylists(playlistContainer, playlists, position);
	    }
        /// <summary>
        /// Add multiple playlists to a playlist container.
        /// </summary>
        /// <param name="playlistContainer">The playlist container.</param>
        /// <param name="playlists">A List of playlists to be added.</param>
        /// <param name="position">The target position of the added playlists.</param>
        /// <returns></returns>
        public bool PlaylistContainerAddPlaylists(PlaylistContainer playlistContainer, List<Playlist> playlists, int position)
        {
		    string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;
		    /* Add the playlists for new checksum calculation. */
            playlistContainer.Playlists.InsertRange(position, playlists);

		    /* Build a comma separated list of tracks and append '01' to every id!. */
		    string playlistList = "";

		    for(int i = 0; i < playlists.Count; i++){
			    playlistList += ((i > 0)?",":"") + playlists[i].Id + "02";
		    }

		    /* Create XML builder. */
            string xml = @"
			    <change>
				    <ops>
					    <add>
						    <i> + " + position.ToString() + @"</i>
						    <items>" + playlistList + @"</items>
					    </add>
				    </ops>
				    <time>" + timestamp.ToString() + @"</time>
				    <user>" + user + @"</user>
			    </change>
			    <version>" + 
                           (playlistContainer.Revision + 1).ToString("0000000000")+ "," +
                    playlistContainer.Playlists.Count.ToString("0000000000") + "," +
                    playlistContainer.Checksum.ToString("0000000000") + ",0</version>";


		    /* Remove the playlists because we need to validate the checksum again. */
            playlistContainer.Playlists.RemoveRange(position, playlists.Count);

		    /* Create channel callback */
		    ChannelCallback callback = new ChannelCallback();

		    /* Send change playlist request. */
		    try
            {
			    this.protocol.SendChangePlaylistContainer(callback, playlistContainer, xml);
		    }
		    catch(ProtocolException e)
            {
			    return false;
		    }

		    /* Get response. */
		    byte[] data = callback.Get(this.timeout);

		    /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

		    if(confirmation == null)
            {
			    return false;
		    }

		    /* Add the tracks, since operation was successful. */
		    playlistContainer.Playlists.InsertRange(position, playlists);

		    /* Set new revision. */
		    playlistContainer.Revision = confirmation.Revision;

		    return true;
	    }
        /// <summary>
        /// Remove a playlist from a playlist container.
        /// </summary>
        /// <param name="playlistContainer">The playlist container.</param>
        /// <param name="position">The position of the playlist to remove.</param>
        /// <returns></returns>
        public bool PlaylistContainerRemovePlaylist(PlaylistContainer playlistContainer, int position)
        {
		    return this.PlaylistContainerRemovePlaylists(playlistContainer, position, 1);
	    }
        /// <summary>
        /// Remove multiple playlists from a playlist container.
        /// </summary>
        /// <param name="playlistContainer">The playlist container.</param>
        /// <param name="position">The position of the tracks to remove.</param>
        /// <param name="count">The number of track to remove.</param>
        /// <returns></returns>
        public bool PlaylistContainerRemovePlaylists(PlaylistContainer playlistContainer, int position, int count)
        {
		    string user      = this.session.StringUsername;
		    long   timestamp = DateTime.Now.Ticks;

		    /* Create a sublist view (important!). */
		    List<Playlist> playlists = new List<Playlist>();
            playlists = playlistContainer.Playlists.GetRange(position, count);
            

		    /* First remove the playlist(s) to calculate the new checksum. */
            playlistContainer.Playlists.RemoveRange(position, count);

		    /* Create XML builder. */
		    string xml = @"
			    <change>
				    <ops>
					    <del>
						    <i>" + position.ToString() + @"</i>
						    <k>" + count.ToString() + @"</k>
					    </del>
				    </ops>
				    <time>" + timestamp.ToString() + @"</time>
				    <user>" + user + @"</user>
			    </change>
			    <version>" + (playlistContainer.Revision + 1).ToString("0000000000") + "," +
                    playlistContainer.Playlists.Count.ToString("0000000000") + "," +
                    playlistContainer.Checksum.ToString("0000000000") + ",0</version>";

		    /* Add the playlist(s) again, because we need the old checksum for sending. */
		    playlistContainer.Playlists.InsertRange(position, playlists);

		    /* Create channel callback */
		    ChannelCallback callback = new ChannelCallback();

		    /* Send change playlist request. */
		    try{
			    this.protocol.SendChangePlaylistContainer(callback, playlistContainer, xml.ToString());
		    }
		    catch(ProtocolException e){
			    return false;
		    }

		    /* Get response. */
		    byte[] data = callback.Get(this.timeout);

		    /* Check confirmation. */
		    PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

		    if(confirmation == null)
            {
			    return false;
		    }

		    /* Remove the playlist(s), since operation was successful. */
            playlistContainer.Playlists.RemoveRange(position, count);

		    /* Set new revision. */
		    playlistContainer.Revision = confirmation.Revision;

		    return true;
	    }
        /// <summary>
        /// Get a playlist.
        /// </summary>
        /// <param name="id">Id of the playlist to load.</param>
        /// <param name="cached">Whether to use a cached version if available or not.</param>
        /// <returns></returns>
        public Playlist Playlist(string id, bool cached)
        {
		    /*
		     * Check if id is a 32-character hex string,
		     * if not try to parse it as a Spotify URI.
		     */
		    if(id.Length != 32 && !Hex.IsHex(id))
            {
			    try
                {
				    Link link = Link.Create(id);

				    if(!link.IsPlaylistLink)
                    {
					    throw new ArgumentException("Given Spotify URI is not a playlist URI.");
				    }

				    id = link.Id;
			    }
			    catch(InvalidSpotifyURIException e)
                {
				    throw new ArgumentException("Given id is neither a 32-character hex string nor a valid Spotify URI.");
			    }
		    }

		    /* Data buffer. */
		    byte[] data;

		    if(cached && this.cache != null && this.cache.Contains("playlist", id))
            {
			    data = this.cache.Load("playlist", id);
		    }
		    else
            {
			    /* Create channel callback */
			    ChannelCallback callback = new ChannelCallback();

			    /* Send playlist request. */
			    try
                {
				    this.protocol.SendPlaylistRequest(callback, id);
			    }
			    catch(ProtocolException e)
                {
				    return null;
			    }

			    /* Get data. */
			    data = callback.Get(this.timeout);

			    /* Save data to cache. */
			    if(this.cache != null)
                {
				    this.cache.Store("playlist", id, data);
			    }
		    }

		    /* Create and return playlist. */
            return XmlPlaylistParser.ParsePlaylist(data, id);
	    }
        /// <summary>
        /// Get a playlist.
        /// </summary>
        /// <param name="id">d of the playlist to load.</param>
        /// <returns></returns>
        public Playlist Playlist(string id)
        {
		    return this.Playlist(id, false);
	    }
        /// <summary>
        /// Create a playlist.
        /// </summary>
        /// <param name="name">The name of the playlist to create.</param>
        /// <returns>A <see cref="Playlist"/> object or null on failure.</returns>
        public Playlist PlaylistCreate(string name)
        {
		    return this.PlaylistCreate(name, false, null, null);
	    }
        /// <summary>
        /// Create a playlist.
        /// </summary>
        /// <param name="sourceAlbum">An <see cref="Album"/> object</param>
        /// <returns>A <see cref="Playlist"/> object or null on failure.</returns>
        public Playlist PlaylistCreate(Album sourceAlbum) 
        {
		    /* Browse album. */
		    Album  album       = this.Browse(sourceAlbum);
		    string name        = album.Artist.Name + " - " + album.Name;
		    string description = "Released in " + album.Year;

		    /* Create playlist from album. */
		    Playlist playlist = this.PlaylistCreate(name, false, description, album.Cover);

		    if(playlist != null && this.PlaylistAddTracks(playlist, album.Tracks, 0))
            {
			    return playlist;
		    }
		    else
            {
			    this.PlaylistDestroy(playlist);
		    }

		    return playlist;
	    }
        /// <summary>
        /// Create a playlist.
        /// <remarks>This just creates a playlist,
        /// but doesn't add it to the playlist container!</remarks>
        /// </summary>
        /// <param name="name">The name of the playlist to create.</param>
        /// <param name="collaborative">If the playlist shall be collaborative.</param>
        /// <param name="description">A description of the playlist.</param>
        /// <param name="picture">An image id to associate with this playlist.</param>
        /// <returns>A <see cref="Playlist"/> object or null on failure.</returns>
        public Playlist PlaylistCreate(string name, bool collaborative, string description, string picture)
        {
            string id = Hex.ToHex(RandomBytes.GetRandomBytes(16));
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;
            Playlist playlist = new Playlist(id, name, user, collaborative);

            /* Create XML builder. */
            string xml = @"
                <id-is-unique/>
                <change>
                    <ops>
                        <create/>
                        <name>" + name + @"</name>
                        <description>" + description + @"</description>
                        <picture>" + picture + @"</picture>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>0000000001,0000000000,0000000001," + ((collaborative) ? "1" : "0") + "</version>";

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendCreatePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return null;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return null;
            }

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return playlist;
        }
        /// <summary>
        /// Destroy a playlist.
        /// </summary>
        /// <param name="playlist">The playlist to destroy.</param>
        /// <returns>true if the playlist was successfully destroyed, false otherwise.</returns>
        public bool PlaylistDestroy(Playlist playlist)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <destroy/>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000")+ "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            return true;
        }
        /// <summary>
        /// Add a track to a playlist.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="track">The track to be added.</param>
        /// <returns></returns>
        public bool PlaylistAddTrack(Playlist playlist, Track track)
        {
            return this.PlaylistAddTrack(playlist, track, playlist.Tracks.Count);
        }
        /// <summary>
        /// Add a track to a playlist.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="track">The track to be added.</param>
        /// <param name="position">The target position of the added track.</param>
        /// <returns>true on success and false on failure.</returns>
        public bool PlaylistAddTrack(Playlist playlist, Track track, int position)
        {
            List<Track> tracks = new List<Track>();
            tracks.Add(track);
            return this.PlaylistAddTracks(playlist, tracks, position);
        }
        /// <summary>
        /// Add multiple tracks to a playlist.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="tracks">A List of tracks to be added.</param>
        /// <param name="position">The target position of the added track.</param>
        /// <returns>true on success and false on failure.</returns>
        public bool PlaylistAddTracks(Playlist playlist, List<Track> tracks, int position)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (!playlist.IsCollaborative && playlist.Author != user)
            {
                return false;
            }

            /* Add the tracks for new checksum calculation. */
            playlist.Tracks.InsertRange(position, tracks);

            /* Build a comma separated list of tracks and append '01' to every id!. */
            string trackList = "";

            for (int i = 0; i < tracks.Count; i++)
            {
                trackList += ((i > 0) ? "," : "") + tracks[i].Id + "01";
            }

            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <add>
                            <i>" + position + @"</i>
                            <items>" + trackList + @"</items>
                        </add>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Remove the tracks again. */
            playlist.Tracks.RemoveRange(position, tracks.Count);

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            /* Add the tracks, since operation was successful. */
            playlist.Tracks.InsertRange(position, tracks);

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }
        /// <summary>
        /// Remove a track from a playlist.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="position">The position of the track to remove.</param>
        /// <returns>true on success and false on failure.</returns>
        public bool PlaylistRemoveTrack(Playlist playlist, int position)
        {
            return this.PlaylistRemoveTracks(playlist, position, 1);
        }
        /// <summary>
        /// Remove multiple tracks from a playlist.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="position">The position of the tracks to remove.</param>
        /// <param name="count">The number of track to remove.</param>
        /// <returns>true on success and false on failure.</returns>
        public bool PlaylistRemoveTracks(Playlist playlist, int position, int count)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (!playlist.IsCollaborative && playlist.Author != user)
            {
                return false;
            }

            /* Create a sublist view (important!) and clone it by constructing a new ArrayList. */
            List<Track> tracks = new List<Track>();
            tracks.AddRange(playlist.Tracks.GetRange(position, count));
            
            /* First remove the track(s) to calculate the new checksum. */
            playlist.Tracks.RemoveRange(position, count);

            
            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <del>
                            <i>" + position + @"</i>
                            <k>" + count + @"</k>
                        </del>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Add the track(s) again, because we need the old checksum for sending. */
		    playlist.Tracks.InsertRange(position, tracks);

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            /* Remove the track(s), since operation was successful. */
            playlist.Tracks.RemoveRange(position, count);

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }
        /// <summary>
        /// Move track position into a play list.
        /// </summary>
        /// <param name="playlist">The playlist.</param>
        /// <param name="sourcePosition">The source Track position.</param>
        /// <param name="destPosition">The destiny Track position.</param>
        /// <returns>true on success and false on failure.</returns>
        public bool PlaylistMoveTrack(Playlist playlist, int sourcePosition, int destPosition)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (!playlist.IsCollaborative && playlist.Author != user)
            {
                return false;
            }

            /* Save the original tracks positions*/
            List<Track> tracks = new List<Track>();
            tracks.AddRange(playlist.Tracks);

            /* First move the track(s) to calculate the new checksum. */
            /* 1. copy the track in the destiny position */
            playlist.Tracks.Insert(destPosition, playlist.Tracks[sourcePosition]);
            /* 2. remove the track in the source position */
            playlist.Tracks.RemoveAt(sourcePosition);


            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <mov>
                            <i>" + sourcePosition + @"</i>
                            <j>" + destPosition + @"</j>
                        </mov>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Set old track list to recalculate the old checksum */
            playlist.Tracks = tracks;

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            /* Move the track(s), since operation was successful. */
            /* 1. copy the track in the destiny position */
            playlist.Tracks.Insert(destPosition, playlist.Tracks[sourcePosition]);
            /* 2. remove the track in the source position */
            playlist.Tracks.RemoveAt(sourcePosition);

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }
        /// <summary>
        /// Rename a playlist.
        /// </summary>
        /// <param name="playlist">The <see cref="Playlist"/> to rename.</param>
        /// <param name="name">The new name for the playlist.</param>
        /// <returns>true on success or false on failure.</returns>
        public bool PlaylistRename(Playlist playlist, string name)
        { 
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (playlist.Author != user)
            {
                return false;
            }

            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <name>" + name + @"</name>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            /* Set name, since operation was successful. */
            playlist.Name = name;

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }
        /// <summary>
        /// Set playlist collaboration.
        /// </summary>
        /// <param name="playlist">The <see cref="Playlist"/> to change.</param>
        /// <param name="collaborative">Whether it should be collaborative or not.</param>
        /// <returns>true on success or false on failure.</returns>
        public bool PlaylistSetCollaborative(Playlist playlist, bool collaborative)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (playlist.Author != user)
            {
                return false;
            }

            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <pub>" + (collaborative? "1" : "0") + @"</pub>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }
        /// <summary>
        /// Set playlist information.
        /// </summary>
        /// <param name="playlist">The <see cref="Playlist"/> to change.</param>
        /// <param name="description">The description to set.</param>
        /// <param name="picture">The picture to set.</param>
        /// <returns>true on success or false on failure.</returns>
        public bool PlaylistSetInformation(Playlist playlist, string description, string picture)
        {
            string user = this.session.StringUsername;
            long timestamp = DateTime.Now.Ticks;

            /* Check if user is allowed to edit playlist. */
            if (playlist.Author != user)
            {
                return false;
            }

            /* Create XML builder. */
            string xml = @"
                <change>
                    <ops>
                        <description>" + description + @"</description>
                        <picture>" + picture + @"</picture>
                    </ops>
                    <time>" + timestamp + @"</time>
                    <user>" + user + @"</user>
                </change>
                <version>" +
                    (playlist.Revision + 1).ToString("0000000000") + "," +
                    playlist.Tracks.Count.ToString("0000000000") + "," +
                    playlist.Checksum.ToString("0000000000") + "," +
                    (playlist.IsCollaborative ? "1" : "0") + "</version>";

            /* Create channel callback */
            ChannelCallback callback = new ChannelCallback();

            /* Send change playlist request. */
            try
            {
                this.protocol.SendChangePlaylist(callback, playlist, xml);
            }
            catch (ProtocolException e)
            {
                return false;
            }

            /* Get response. */
            byte[] data = callback.Get(this.timeout);

            /* Check confirmation. */
            PlaylistConfirmation confirmation = XmlPlaylistParser.ParsePlaylistConfirmation(data);

            if (confirmation == null)
            {
                return false;
            }
            /* Set metadata, since the operation was successful. */
		    playlist.Description = description;
		    playlist.Picture = picture;

            /* Set new revision and collaborative flag. */
            playlist.Revision = confirmation.Revision;
            playlist.IsCollaborative = confirmation.Collaborative;

            return true;
        }


        public MusicStream GetMusicStream(Track track, Sharpotify.Media.File file, TimeSpan timeout)
        {
            ChannelCallback handler = new ChannelCallback();
            ChannelHeaderCallback callback2 = new ChannelHeaderCallback();
            try
            {
                this.protocol.SendPlayRequest();
            }
            catch (ProtocolException)
            {
                return null;
            }
            try
            {
                this.protocol.SendAesKeyRequest(handler, track, file);
            }
            catch (ProtocolException)
            {
                return null;
            }
            byte[] key = handler.Get(timeout);
            try
            {
                this.protocol.SendSubstreamRequest(callback2, file, 0, 0);
            }
            catch (ProtocolException)
            {
                return null;
            }
            if (callback2.Get(this.Timeout).Length == 4)
            {
                throw new NotImplementedException();
            }
            MusicStream output = new MusicStream();
            ChannelStreamer streamer = new ChannelStreamer(this.protocol, file, key, output);
            return output;
        }
        #endregion
        #region ICommandHandler Methods
        public void CommandReceived(int command, byte[] payload)
        {
            byte[] aux;
            switch (command)
            {
                case Command.COMMAND_SECRETBLK:
                    /* Check length. */
                    if (payload.Length != 336)
                    {
                        System.Console.WriteLine("Got command 0x02 with len " + payload.Length  + ", expected 336!\n");
                    }

                    /* Check RSA public key. */
                    byte[] rsaPublicKey = this.session.RSAPublicKey.ToByteArray();

                    for (int i = 0; i < 128; i++)
                    {
                        if (payload[16 + i] != rsaPublicKey[i])
                        {
                            System.Console.WriteLine("RSA public key doesn't match! " + i + "\n");

                            break;
                        }
                    }

                    /* Send cache hash. */
                    try
                    {
                        this.protocol.SendCacheHash();
                    }
                    catch (ProtocolException e)
                    {
                        /* Just don't care. */
                    }

                    break;

                case Command.COMMAND_PING: 
				    /* Ignore the timestamp but respond to the request. */
				    /* int timestamp = IntegerUtilities.bytesToInteger(payload); */
				    try{
					    this.protocol.SendPong();
				    }
				    catch(ProtocolException e){
					    /* Just don't care. */
				    }

				    break;

                case Command.COMMAND_PONGACK:
                    break;

                case Command.COMMAND_CHANNELDATA: 
				    Channel.Process(payload);
                    break;

                case Command.COMMAND_CHANNELERR:
                    Channel.Error(payload);
                    break;

                case Command.COMMAND_AESKEY:
                    aux = new byte[payload.Length - 2];
                    Array.Copy(payload, 2, aux, 0, aux.Length);
                    /* Channel id is at offset 2. AES Key is at offset 4. */
                    Channel.Process(aux);
                    break;

                case Command.COMMAND_SHAHASH:
                    /* Do nothing. */
                    break;

                case Command.COMMAND_COUNTRYCODE: 
				    //System.out.println("Country: " + new String(payload, Charset.forName("UTF-8")));
                    this.user.Country = Encoding.UTF8.GetString(payload, 0, payload.Length);

				    /* Release 'country' permit. */
				    this.userSemaphore.Release();

				    break;
			
			    case Command.COMMAND_P2P_INITBLK: 
				    /* Do nothing. */
				    break;

			    case Command.COMMAND_NOTIFY: 
				    /* HTML-notification, shown in a yellow bar in the official client. */
				    /* Skip 11 byte header... */
				    /*System.out.println("Notification: " + new String(
					    Arrays.copyOfRange(payload, 11, payload.length), Charset.forName("UTF-8")
				    ));*/
                    aux = new byte[payload.Length - 11];
                    Array.Copy(payload, 11, aux, 0, payload.Length - 11);
				    this.user.Notification = Encoding.UTF8.GetString(aux, 0, aux.Length);
				    break;
			    
			    case Command.COMMAND_PRODINFO: 
				    this.user = XmlUserParser.ParseUser(payload, this.user);

				    /* Release 'prodinfo' permit. */
				    this.userSemaphore.Release();

				    break;
			    
			    case Command.COMMAND_WELCOME: 
				    break;
			    
			    case Command.COMMAND_PAUSE: 
				    /* TODO: Show notification and pause. */
				    break;
			    
			    case Command.COMMAND_PLAYLISTCHANGED:
                    System.Console.WriteLine("Playlist '" + Hex.ToHex(payload) + "' changed!\n");

				    break;
			    
			    default:
                    System.Console.WriteLine("Unknown Command: 0x" + command.ToString("X2") + " Length: " + payload.Length + "\n");
                    System.Console.WriteLine("Data: " + Encoding.UTF8.GetString(payload, 0, payload.Length) + " " + Hex.ToHex(payload));

				    break;
            }
        }
        #endregion
    }
}

