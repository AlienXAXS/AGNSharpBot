using SQLite;
using System;

namespace Responses.Commands.GameGiveaway.SQL
{
    internal class GameGiveawayUserDb
    {
        // Used to remember the last time a user got a free game.
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long DiscordId { get; set; }
        public DateTime DateTime { get; set; }

        public bool isHumbleRegistered { get; set; }
    }
}