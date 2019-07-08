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
using GlobalLogger.AdvancedLogger;
using Interface;
using PluginManager;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPlugin
    {
        public EventRouter EventRouter { get; set; }
        public string Name => "GameWatcher";


        private DiscordSocketClient _discordClient;

        public void ExecutePlugin()
        {
            try
            {
                _discordClient = EventRouter.GetDiscordSocketClient();

                AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                    .SetRetentionOptions(new RetentionOptions() {Compress = true});
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER] Checking Discord...");
                CheckDiscord().GetAwaiter().GetResult();
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER]  >  Complete");

                CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Control>();
                EventRouter.GuildMemberUpdated += DiscordClientOnGuildMemberUpdatedEvent;
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
            foreach (var guild in _discordClient.Guilds)
            {

                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER]   > Checking guild {guild.Name}...");

                // Clean all roles
                var foundRoles = guild.Roles.Where(y => y.Name.StartsWith("Playing:"));
                foreach (var role in foundRoles)
                {
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER]   > Pre-Delete Role {role.Name}...");
                    await role.DeleteAsync();
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"Found old role of {role.Name}. I have deleted the role");
                }

                // Rescan all users
                foreach (var user in guild.Users)
                {
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"Scanning user {user.Username}");
                    DiscordClientOnGuildMemberUpdated(user, user, true).Wait();
                }
            }
        }

        private async Task DiscordClientOnGuildMemberUpdated(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser, bool firstRun = false)
        {
            await GameHandler.Instance.GameScan(oldGuildUser, newGuildUser, firstRun);
        }

        public void Dispose()
        {
            
        }
    }
}
