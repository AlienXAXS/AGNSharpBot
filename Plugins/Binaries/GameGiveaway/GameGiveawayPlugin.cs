using System.ComponentModel.Composition;
using CommandHandler;
using GameGiveaway.Commands;
using Interface;
using InternalDatabase;
using PluginManager;
using Responses.Commands.GameGiveaway.SQL;

namespace GameGiveaway
{
    [Export(typeof(IPlugin))]
    public class GameGiveawayPlugin : IPlugin
    {
        string IPlugin.Name => "GameGiveaway";
        public EventRouter EventRouter { get; set; }

        public string Version => "0.1";
        string IPlugin.Description => "Some useful admin utilities and commands.";

        void IPlugin.ExecutePlugin()
        {
            // SQL Database Setup
            Handler.Instance.GetConnection().RegisterTable<GameGiveawayGameDb>();
            Handler.Instance.GetConnection().RegisterTable<GameGiveawayUserDb>();

            // Register our commands with the handler
            HandlerManager.Instance.RegisterHandler<GameGiveawayAdmin>();
            HandlerManager.Instance.RegisterHandler<GameGiveawayPublic>();
        }

        void IPlugin.Dispose()
        {
        }
    }
}