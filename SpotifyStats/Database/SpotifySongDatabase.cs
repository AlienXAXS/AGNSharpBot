using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlobalLogger;
using Newtonsoft.Json;

namespace SpotifyStats.Database
{

    class SpotifySong
    {
        public string Artist { get; set; }
        public string TrackName { get; set; }
        public ulong PlayCount { get; set; }

        public SpotifySong(string artist, string trackName)
        {
            Artist = artist;
            TrackName = trackName;
            PlayCount = 1;
        }
    }

    class SpotifySongDatabase
    {
        // Instancing
        private static SpotifySongDatabase _instance;
        public static SpotifySongDatabase Instance = _instance ?? (_instance = new SpotifySongDatabase());

        private List<SpotifySong> _spotifySongs = new List<SpotifySong>();

        private const string ConfigurationPath = "Plugins\\Config\\SpotifySongs.json";

        public SpotifySong AddTrack(string artist, string trackName)
        {
            var foundSong = FindSongByArtistAndName(artist, trackName);

            if (foundSong != null)
            {
                //We have the song in our database already, add a new listen count to it
                foundSong.PlayCount++;
            }
            else
            {
                // New song, add it to the database
                _spotifySongs.Add(new SpotifySong(artist, trackName));
                foundSong = _spotifySongs[_spotifySongs.Count-1];
            }

            // Save the database to file
            #pragma warning disable 4014
            SaveDatabase();
            #pragma warning restore 4014

            return foundSong;
        }

        public void RemoveTrack()
        {
            // Most likely never needed...
        }

        public SpotifySong FindSongByArtistAndName(string artist, string trackName)
        {
            return _spotifySongs.Where(x => x.Artist.Equals(artist) && x.TrackName.Equals(trackName))
                .DefaultIfEmpty(null).FirstOrDefault();
        }

        private async Task SaveDatabase()
        {
            var jsonOutput = JsonConvert.SerializeObject(_spotifySongs, Formatting.Indented);
            try
            {
                System.IO.File.WriteAllText(ConfigurationPath, jsonOutput);
            }
            catch (Exception ex)
            {
                await Logger.Instance.Log($"Unable to write to SpotifySongs.json, error was:\r\n{ex.Message}\r\n\r\n{ex.StackTrace}", Logger.LoggerType.ConsoleOnly);
            }
        }

        private async Task LoadDatabase()
        {
            try
            {
                // Do not load our config if the file does not exist
                if (!System.IO.File.Exists(ConfigurationPath)) return;

                _spotifySongs =
                    JsonConvert.DeserializeObject<List<SpotifySong>>(System.IO.File.ReadAllText(ConfigurationPath));
                await Logger.Instance.Log($"Loaded {_spotifySongs.Count} spotify songs from the database",
                    Logger.LoggerType.ConsoleAndDiscord);
            }
            catch (Exception ex)
            {
                await Logger.Instance.Log($"Unable to parse SpotifySongs.json, error was:\r\n{ex.Message}", Logger.LoggerType.ConsoleOnly);
            }
        }

        public async Task LoadTracks()
        {
            await LoadDatabase();
        }
    }
}
