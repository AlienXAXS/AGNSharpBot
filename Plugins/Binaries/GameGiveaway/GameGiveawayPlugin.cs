using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameGiveaway.Commands;
using Interface;
using PluginManager;

namespace GameGiveaway
{
    [Export(typeof(IPlugin))]
    public class GameGiveawayPlugin : IPlugin
    {
        string IPlugin.Name => "GameGiveaway";
        public EventRouter EventRouter { get; set; }

        string IPlugin.Description => "Some useful admin utilities and commands.";
        
        void IPlugin.ExecutePlugin()
        {
            // SQL Database Setup
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Responses.Commands.GameGiveaway.SQL.GameGiveawayGameDb>();
            InternalDatabase.Handler.Instance.GetConnection().RegisterTable<Responses.Commands.GameGiveaway.SQL.GameGiveawayUserDb>();

            // Register our commands with the handler
            CommandHandler.HandlerManager.Instance.RegisterHandler<GameGiveawayAdmin>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<GameGiveawayPublic>();
        }

        void IPlugin.Dispose()
        {
        }
    }
}
