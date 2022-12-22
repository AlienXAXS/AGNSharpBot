using System.ComponentModel.Composition;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;
using Interface;
using InternalDatabase;
using JoinQuitMessages.Configuration;
using PluginManager;

namespace JoinQuitMessages
{
    [Export(typeof(IPlugin))]
    public class PluginJoinQuitMessages : IPlugin
    {
        public string Name => "JoinQuitMessages";

        public string Version => "0.1";
        public string Description => "Logs when people join and quit your discord server into a channel of your choice";

        public void ExecutePlugin()
        {
            // Register our connection, and our table.
            Handler.Instance.NewConnection()?.RegisterTable<SQLTables.Configuration>();

            // Add our commands to the cmdhandler
            HandlerManager.Instance.RegisterHandler<DiscordConfigurationHandler>();

            // Setup our event router
            EventRouter.UserJoined += EventRouterOnUserJoined;
            EventRouter.UserLeft += EventRouterOnUserLeft;
        }

        public void Dispose()
        {
            Handler.Instance.GetConnection().DbConnection.Close();
        }

        public EventRouter EventRouter { get; set; }

        private Task EventRouterOnUserLeft(SocketGuildUser user)
        {
            var foundConfiguration = ConfigurationHandler.Instance.GetConfiguration(user.Guild.Id);
            if (foundConfiguration == null) return Task.CompletedTask;

            var foundChannel = user.Guild.GetChannel((ulong) foundConfiguration.ChannelId);
            if (foundChannel is ISocketMessageChannel sktChannel)
                sktChannel.SendMessageAsync(
                    $"User {user.Username} has left the guild, they were a member since {user.JoinedAt}.");

            return Task.CompletedTask;
        }

        private Task EventRouterOnUserJoined(SocketGuildUser user)
        {
            var foundConfiguration = ConfigurationHandler.Instance.GetConfiguration(user.Guild.Id);
            if (foundConfiguration == null) return Task.CompletedTask;

            var foundChannel = user.Guild.GetChannel((ulong) foundConfiguration.ChannelId);
            if (foundChannel is ISocketMessageChannel sktChannel)
                sktChannel.SendMessageAsync($"User {user.Username} has joined the guild");

            return Task.CompletedTask;
        }
    }
}