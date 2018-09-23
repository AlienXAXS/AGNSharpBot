using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace Responses.Informational
{
    class LastOnline
    {
        [Command("lo", "lo <userid> - Returns when a user was last seen online.")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        [Alias("lastonline","online")]
        public void CheckLastOnline(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            
        }
    }
}