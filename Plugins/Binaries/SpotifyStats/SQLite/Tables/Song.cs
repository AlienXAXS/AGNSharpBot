using SQLite;

namespace SpotifyStats.SQLite.Tables
{
    public class Song
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string SongId { get; set; }

        public string Artist { get; set; }
        public string Name { get; set; }
    }
}