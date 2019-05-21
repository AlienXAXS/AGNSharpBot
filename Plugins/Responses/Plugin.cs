using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using PluginInterface;
using Discord.WebSocket;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Discord Responses";
        DiscordSocketClient IPlugin.DiscordClient { get; set; }

        void IPlugin.ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"Responses.dll Plugin Loading...");

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
            GlobalLogger.Logger.Instance.WriteConsole("Responses Disposed");
        }
    }
}
