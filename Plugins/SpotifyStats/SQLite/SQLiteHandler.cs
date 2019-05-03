using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLitePCL;

namespace SpotifyStats.SQLite
{

    class SongEntry
    {
        public Tables.Song Song;
        public List<Tables.Listener> Listeners;
    }

    class SqLiteHandler
    {
        // Instanced
        private static SqLiteHandler _instance;
        public static SqLiteHandler Instance = _instance ?? (_instance = new SqLiteHandler());

        private SQLiteConnection _connection;
        private const string DatabasePath = "Plugins\\Config\\SpotifyStats.db";

        public void InitDatabase()
        {
            if (!System.IO.File.Exists(DatabasePath))
                System.Data.SQLite.SQLiteConnection.CreateFile(DatabasePath);

            _connection = new SQLiteConnection(DatabasePath);

            // Create our tables
            _connection.CreateTable<Tables.Listener>();
            _connection.CreateTable<Tables.Song>();
        }

        public SQLiteConnection GetConnection() => _connection;

        public SongEntry AddSongAndListener(string songId, string artist, string name, ulong discordId)
        {
            var song = _connection.Table<Tables.Song>().Where(x => x.SongId.Equals(songId)).DefaultIfEmpty(null)
                .FirstOrDefault();

            if (song == null)
            {
                song = new Tables.Song()
                {
                    Artist = artist,
                    SongId = songId,
                    Name = name
                };
                _connection.Insert(song);
            }

            var listener = new Tables.Listener()
            {
                SongId = song.Id,
                DiscordId = (long) discordId,
                DateTime = DateTime.Now
            };
            _connection.Insert(listener);

            return new SongEntry()
            {
                Song = song,
                Listeners = _connection.Table<Tables.Listener>().Where(x => x.SongId == song.Id).ToList()
            };
        }
    }
}
