using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using HtmlAgilityPack;
using PluginInterface;

namespace GameUpdateNotifier
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "GameUpdateNotifier";
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
