using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using PluginInterface;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using Responses.Informational;
using Responses.SQLTables;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Discord Responses";
        public DiscordSocketClient DiscordClient { get; set; }


        void IPlugin.ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() {Compress = true});
            AdvancedLoggerHandler.Instance.GetLogger().Log($"Responses.dll Plugin Loading...");

            // SQL Database Setup
            // Last Online
            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<LastOnlineTable>();
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Commands.GameGiveaway.SQL.GameGiveawayGameDb>();
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Commands.GameGiveaway.SQL.GameGiveawayUserDb>();

            var lastOnlineHandler = new LastOnlineDbHandler();
            lastOnlineHandler.StartOnlineScanner(DiscordClient);

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Informational.LastOnline>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AuthorisedCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminPermissions>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.CatCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.DogCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.ASCIIArt>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.GameGiveaway.GameGiveawayAdmin>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.GameGiveaway.GameGiveawayPublic>();
        }

        void IPlugin.Dispose()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("Responses Disposed");
        }
    }
}
