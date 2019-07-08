using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PluginManager.SQL
{
    class PluginManager
    {
        [PrimaryKey]
        public int id { get; set; }
        public string PluginName { get; set; }
        public bool Enabled { get; set; }
        public long GuildId { get; set; }
    }
}
