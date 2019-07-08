using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using HtmlAgilityPack;
using Interface;
using PluginManager;

namespace GameUpdateNotifier
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public EventRouter EventRouter { get; set; }
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
