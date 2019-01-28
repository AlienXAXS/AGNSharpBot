using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using PluginInterface;
using Discord.WebSocket;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        string IPlugin.Name => "Discord Responses";
        DiscordSocketClient IPlugin.DiscordClient { get; set; }
        List<string> IPlugin.Commands => new List<string> { "test_plugin" };

        List<PluginRequestTypes.PluginRequestType> IPlugin.RequestTypes =>
            new List<PluginRequestTypes.PluginRequestType> {PluginRequestTypes.PluginRequestType.COMMAND};

        void IPlugin.ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"Responses.dll Plugin Loading...");

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Informational.LastOnline>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AuthorisedCommands>();
        }

        Task IPlugin.CommandAsync(string command, string message, SocketMessage sktMessage)
        {
            sktMessage.Channel.SendMessageAsync("Plugin Response Message!");
            return Task.CompletedTask;
        }

        Task IPlugin.Message(string message, SocketMessage sktMessage)
        {
            return Task.CompletedTask;
        }

        void IPlugin.Dispose()
        {
            GlobalLogger.Logger.Instance.WriteConsole("Responses Disposed");
        }
    }
}
