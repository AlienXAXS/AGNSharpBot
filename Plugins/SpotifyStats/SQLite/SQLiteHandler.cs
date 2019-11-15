using InternalDatabase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifyStats.SQLite
{
    internal class SongEntry
    {
        public Tables.Song Song;
        public List<Tables.Listener> Listeners;
    }

    internal static class SQLiteHandler
    {
        public static SongEntry AddSongAndListener(Connection connection, string songId, string artist, string name, ulong discordId)
        {
            var song = connection.DbConnection.Table<Tables.Song>().Where(x => x.SongId.Equals(songId)).DefaultIfEmpty(null)
                .FirstOrDefault();

            if (song == null)
            {
                song = new Tables.Song()
                {
                    Artist = artist,
                    SongId = songId,
                    Name = name
                };
                connection.DbConnection.Insert(song);
            }

            var listener = new Tables.Listener()
            {
                SongId = song.Id,
                DiscordId = (long)discordId,
                DateTime = DateTime.Now
            };
            connection.DbConnection.Insert(listener);

            return new SongEntry()
            {
                Song = song,
                Listeners = connection.DbConnection.Table<Tables.Listener>().Where(x => x.SongId == song.Id).ToList()
            };
        }
    }
}