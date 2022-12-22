using System.ComponentModel.Composition;
using CommandHandler;
using Interface;
using InternalDatabase;
using PluginManager;
using Responses.Commands;
using Responses.GhostPingDetector;
using Responses.Informational;
using Responses.SQLTables;

namespace Responses
{
    [Export(typeof(IPlugin))]
    public sealed class PluginResponses : IPlugin
    {
        string IPlugin.Name => "AdminUtilities";
        public string Version => "0.2";
        public EventRouter EventRouter { get; set; }

        string IPlugin.Description => "Provides the guild with useful admin utilities, plus pictures of cats!";

        void IPlugin.ExecutePlugin()
        {
            // SQL Database Setup
            // Last Online
            Handler.Instance.NewConnection().RegisterTable<LastOnlineTable>();

            var lastOnlineHandler = new LastOnlineDbHandler();
            lastOnlineHandler.StartOnlineScanner(EventRouter);

            // Register our commands with the handler
            HandlerManager.Instance.RegisterHandler<AdminCommands>();
            HandlerManager.Instance.RegisterHandler<LastOnline>();
            HandlerManager.Instance.RegisterHandler<AuthorisedCommands>();
            HandlerManager.Instance.RegisterHandler<AdminPermissions>();
            HandlerManager.Instance.RegisterHandler<ModerateUser>();


            GhostPingHandler handler = new GhostPingHandler();
            EventRouter.MessageReceived += async message =>
            {
                await handler.ClientOnMessageReceived(message);
            };

        }

        void IPlugin.Dispose()
        {
        }
    }
}