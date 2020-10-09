using System;
using System.Collections.Generic;
using System.Linq;
using InternalDatabase;
using SpotifyStats.SQLite.Tables;

namespace SpotifyStats.SQLite
{
    internal class SongEntry
    {
        public List<Listener> Listeners;
        public Song Song;
    }

    internal static class SQLiteHandler
    {
        public static SongEntry AddSongAndListener(Connection connection, string songId, string artist, string name,
            ulong discordId)
        {
            var song = connection.DbConnection.Table<Song>().Where(x => x.SongId.Equals(songId)).DefaultIfEmpty(null)
                .FirstOrDefault();

            if (song == null)
            {
                song = new Song
                {
                    Artist = artist,
                    SongId = songId,
                    Name = name
                };
                connection.DbConnection.Insert(song);
            }

            var listener = new Listener
            {
                SongId = song.Id,
                DiscordId = (long) discordId,
                DateTime = DateTime.Now
            };
            connection.DbConnection.Insert(listener);

            return new SongEntry
            {
                Song = song,
                Listeners = connection.DbConnection.Table<Listener>().Where(x => x.SongId == song.Id).ToList()
            };
        }
    }
}