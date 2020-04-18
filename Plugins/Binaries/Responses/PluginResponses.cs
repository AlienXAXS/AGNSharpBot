using GlobalLogger.AdvancedLogger;
using Interface;
using PluginManager;
using Responses.SQLTables;
using System.ComponentModel.Composition;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public sealed class PluginResponses : IPlugin
    {
        string IPlugin.Name => "AdminUtilities";
        public EventRouter EventRouter { get; set; }

        string IPlugin.Description => "Provides the guild with useful admin utilities, plus pictures of cats!";

        void IPlugin.ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() { Compress = true });
            AdvancedLoggerHandler.Instance.GetLogger().Log($"Responses.dll Plugin Loading...");

            // SQL Database Setup
            // Last Online
            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<LastOnlineTable>();
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Commands.GameGiveaway.SQL.GameGiveawayGameDb>();
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Commands.GameGiveaway.SQL.GameGiveawayUserDb>();

            var lastOnlineHandler = new Informational.LastOnlineDbHandler();
            lastOnlineHandler.StartOnlineScanner(EventRouter);

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Informational.LastOnline>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AuthorisedCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminPermissions>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.CatCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.DogCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Global.ASCIIArt>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.ModerateUser>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.GameGiveaway.GameGiveawayAdmin>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.GameGiveaway.GameGiveawayPublic>();
        }

        void IPlugin.Dispose()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("Responses Disposed");
        }
    }
}