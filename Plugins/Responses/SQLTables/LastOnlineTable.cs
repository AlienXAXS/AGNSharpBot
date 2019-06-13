using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Responses.SQLTables
{
    class LastOnlineTable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public long DiscordId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
