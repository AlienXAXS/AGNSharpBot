using SQLite;
using System;
using System.ComponentModel;

namespace Responses.SQLTables
{
    internal class LastOnlineTable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [DefaultValue(-1)]
        public long DiscordId { get; set; }
        public DateTime DateTime { get; set; }
    }
}