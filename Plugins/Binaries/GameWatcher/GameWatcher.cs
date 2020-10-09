using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;
using GameWatcher.Commands;
using Interface;
using PluginManager;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPluginWithRouter
    {
        private DiscordSocketClient _discordClient;
        public EventRouter EventRouter { get; set; }
        public string Name => "GameWatcher";
        public PluginRouter PluginRouter { get; set; }

        public string Description =>
            "Watches the guild for game activity, and automatically assigns roles to people playing games.";

        public void ExecutePlugin()
        {
            try
            {
                _discordClient = EventRouter.GetDiscordSocketClient();
                GameHandler.Instance.DiscordSocketClient = _discordClient;
                GameHandler.Instance.PluginRouter = PluginRouter;
                GameHandler.Instance.StartGameWatcherTimer();

                HandlerManager.Instance.RegisterHandler<Control>();
                EventRouter.GuildMemberUpdated += DiscordClientOnGuildMemberUpdatedEvent;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public void Dispose()
        {
        }

        private async Task DiscordClientOnGuildMemberUpdatedEvent(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            await GameHandler.Instance.GameScan(arg1, arg2);
        }
    }
}