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
            // SQL Database Setup
            // Last Online
            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<LastOnlineTable>();
            

            var lastOnlineHandler = new Informational.LastOnlineDbHandler();
            lastOnlineHandler.StartOnlineScanner(EventRouter);

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Informational.LastOnline>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AuthorisedCommands>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.AdminPermissions>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.ModerateUser>();
        }

        void IPlugin.Dispose()
        {
        }
    }
}