using SQLite;
using System;

namespace SpotifyStats.SQLite.Tables
{
    public class Listener
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long DiscordId { get; set; }
        public int SongId { get; set; }
        public DateTime DateTime { get; set; }
    }
}