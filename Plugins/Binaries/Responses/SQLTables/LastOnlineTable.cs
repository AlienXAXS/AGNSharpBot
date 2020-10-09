using System;
using System.ComponentModel;
using SQLite;

namespace Responses.SQLTables
{
    internal class LastOnlineTable
    {
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        [DefaultValue(-1)] public long DiscordId { get; set; }

        public DateTime DateTime { get; set; }
    }
}