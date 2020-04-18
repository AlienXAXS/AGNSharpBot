using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGWeekly.Game
{
    class Player
    {
        public ulong DiscordId;
        public string Name;
        public int Team;
        public Player(string name, ulong discordId)
        {
            Name = name;
            DiscordId = discordId;
        }
    }
}
