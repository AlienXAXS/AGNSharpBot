using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace JoinQuitMessages.SQLTables
{
    class Configuration
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public long GuildId { get; set; }
        public long ChannelId { get; set; }
    }
}
