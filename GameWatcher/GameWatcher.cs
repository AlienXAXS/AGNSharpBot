using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord.WebSocket;
using PluginInterface;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPlugin
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public string Name => "GameWatcher";

        public void ExecutePlugin()
        {
            DiscordClient.GuildMemberUpdated += DiscordClientOnGuildMemberUpdated;
        }

        private async Task DiscordClientOnGuildMemberUpdated(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser)
        {
            try
            {
                // As we're dealing with adding/deleting roles, we should make this thread-safe to ensure roles exist before adding people to a role.
                // Reason for this, is if multiple people start a game at the same time, we must execute them one at a time.
                await SemaphoreSlim.WaitAsync();



            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public DiscordSocketClient DiscordClient { get; set; }
        public void Dispose()
        {
            
        }
    }
}
