using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Responses.Commands.GameGiveaway.SQL
{
    class GameGiveawayUserDb
    {
        // Used to remember the last time a user got a free game.
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public long DiscordId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
