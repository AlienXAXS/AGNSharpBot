using Discord.WebSocket;
using Interface;
using PluginManager;
using System.ComponentModel.Composition;

namespace GameUpdateNotifier
{
    [Export(typeof(IPlugin))]
    public class PluginGameUpdateNotifier : IPlugin
    {
        public EventRouter EventRouter { get; set; }
        public string Name => "GameUpdateNotifier";
        public string Description => "Game Update Notifier - Does nothing, is a test plugin";
        public DiscordSocketClient DiscordClient { get; set; }

        public void ExecutePlugin()
        {
            var logger = GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);
            logger.Log("GameUpdateNotifier Plugin Executing...");

            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.test>();
        }

        public void Dispose()
        {
        }
    }
}