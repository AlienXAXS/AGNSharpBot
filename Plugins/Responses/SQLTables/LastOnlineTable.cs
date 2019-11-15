using SQLite;
using System;

namespace Responses.SQLTables
{
    internal class LastOnlineTable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long DiscordId { get; set; }
        public DateTime DateTime { get; set; }
    }
}