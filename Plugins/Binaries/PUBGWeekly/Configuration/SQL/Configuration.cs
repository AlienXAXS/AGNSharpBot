using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGWeekly.Configuration.SQL
{
    public class Configuration
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long RootCategoryId { get; set; }
        public long LobbyId { get; set; }
        public int TeamChannelCount { get; set; }
        public string TeamChannelSyntax { get; set; }

        public long StatusChannel { get; set; }

        public long GuildId { get; set; }
    }

    public class TeamChannels
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TeamNumber { get; set; }
        public long ChannelId { get; set; }
    }
}
