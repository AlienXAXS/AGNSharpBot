using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLitePCL;

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