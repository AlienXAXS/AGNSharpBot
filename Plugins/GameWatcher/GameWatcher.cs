using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger;
using PluginInterface;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPlugin
    {

        public string Name => "GameWatcher";
        
        public void ExecutePlugin()
        {
            try
            {
                Logger.Instance.Log($"[GAMEWATCHER] Checking Discord...", Logger.LoggerType.ConsoleOnly).Wait();
                CheckDiscord().GetAwaiter().GetResult();
                Logger.Instance.Log($"[GAMEWATCHER]  >  Complete", Logger.LoggerType.ConsoleOnly).Wait();
                CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Control>();
                DiscordClient.GuildMemberUpdated += DiscordClientOnGuildMemberUpdatedEvent;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private async Task DiscordClientOnGuildMemberUpdatedEvent(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            await DiscordClientOnGuildMemberUpdated(arg1, arg2);
        }

        private async Task CheckDiscord()
        {
            // Scan for existing roles with the "Playing:" prefix, and delete them.
            foreach (var guild in DiscordClient.Guilds)
            {

                Logger.Instance.Log($"[GAMEWATCHER]   > Checking guild {guild.Name}...", Logger.LoggerType.ConsoleOnly).Wait();

                // Clean all roles
                var foundRoles = guild.Roles.Where(y => y.Name.StartsWith("Playing:"));
                foreach (var role in foundRoles)
                {
                    Logger.Instance.Log($"[GAMEWATCHER]   > Pre-Delete Role {role.Name}...", Logger.LoggerType.ConsoleOnly).Wait();
                    await role.DeleteAsync();
                    Logger.Instance.Log($"Found old role of {role.Name}. I have deleted the role", Logger.LoggerType.ConsoleOnly).Wait();
                }

                // Rescan all users
                foreach (var user in guild.Users)
                {
                    Logger.Instance.Log($"Scanning user {user.Username}", Logger.LoggerType.ConsoleOnly).Wait();
                    DiscordClientOnGuildMemberUpdated(user, user, true).Wait();
                }
            }
        }


        private async Task DiscordClientOnGuildMemberUpdated(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser, bool firstRun = false)
        {
            GameHandler.Instance.GameScan(oldGuildUser, newGuildUser, firstRun);
        }

        public DiscordSocketClient DiscordClient { get; set; }
        public void Dispose()
        {
            
        }
    }
}
