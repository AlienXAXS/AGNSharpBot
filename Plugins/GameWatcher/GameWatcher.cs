using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using Interface;
using PluginManager;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPluginWithRouter
    {
        public EventRouter EventRouter { get; set; }
        public string Name => "GameWatcher";
        public PluginRouter PluginRouter { get; set; }
        public string Description => "Watches the guild for game activity, and automatically assigns roles to people playing games.";

        private DiscordSocketClient _discordClient;

        public void ExecutePlugin()
        {
            try
            {
                AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true).SetRetentionOptions(new RetentionOptions() { Compress = true });
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER] Checking Discord...");
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER]  >  Complete");

                _discordClient = EventRouter.GetDiscordSocketClient();
                GameHandler.Instance.DiscordSocketClient = _discordClient;
                GameHandler.Instance.PluginRouter = PluginRouter;

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
            await GameHandler.Instance.GameScan(arg1, arg2);
        }

        public void Dispose()
        {
            GameHandler.Instance.Dispose();
        }
    }
}