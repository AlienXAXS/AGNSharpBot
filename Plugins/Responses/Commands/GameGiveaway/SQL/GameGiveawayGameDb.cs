using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Responses.Commands.GameGiveaway.SQL
{
    class GameGiveawayGameDb
    {
        // Stores the actual game keys
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Used { get; set; }
    }
}
