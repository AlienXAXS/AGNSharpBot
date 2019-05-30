using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using PluginInterface;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Discord Responses";
        DiscordSocketClient IPlugin.DiscordClient { get; set; }

        void IPlugin.ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() {Compress = true});
            AdvancedLoggerHandler.Instance.GetLogger().Log($"Responses.dll Plugin Loading...");

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Informational.LastOnline>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AuthorisedCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminPermissions>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.CatCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.DogCommand>();
        }

        void IPlugin.Dispose()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("Responses Disposed");
        }
    }
}
