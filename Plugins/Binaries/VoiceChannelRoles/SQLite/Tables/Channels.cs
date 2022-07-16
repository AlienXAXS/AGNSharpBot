using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace VoiceChannelRoles.SQLite.Tables
{
    internal class Channels
    {
        [PrimaryKey][AutoIncrement] public int Id { get; set; }

        public long GuildId { get; set; }
        public long VoiceChannelId { get; set; }
        public long RoleId { get; set; }
    }
}
