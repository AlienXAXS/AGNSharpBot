using Discord.WebSocket;
using Interface;
using PluginManager;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace JoinQuitMessages
{
    [Export(typeof(IPlugin))]
    public class PluginJoinQuitMessages : IPlugin
    {
        public string Name => "JoinQuitMessages";
        public string Description => "Logs when people join and quit your discord server into a channel of your choice";

        public void ExecutePlugin()
        {
            try
            {
                // Setup the logger
                GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);

                // Register our connection, and our table.
                InternalDatabase.Handler.Instance.NewConnection()?.RegisterTable<SQLTables.Configuration>();

                // Add our commands to the cmdhandler
                CommandHandler.HandlerManager.Instance.RegisterHandler<Configuration.DiscordConfigurationHandler>();

                // Setup our event router
                EventRouter.UserJoined += EventRouterOnUserJoined;
                EventRouter.UserLeft += EventRouterOnUserLeft;
            }
            catch (Exception ex)
            {
                GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Exception in plugin {Name}\r\n{ex.Message}\r\n\r\n\r\n{ex.StackTrace}");

                if (ex.InnerException != null)
                    GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Inner Exception: {ex.InnerException.Message}\r\n\r\n\r\n{ex.InnerException.StackTrace}");
            }
        }

        private Task EventRouterOnUserLeft(SocketGuildUser user)
        {
            var foundConfiguration = Configuration.ConfigurationHandler.Instance.GetConfiguration(user.Guild.Id);
            if (foundConfiguration == null) return Task.CompletedTask;

            var foundChannel = user.Guild.GetChannel((ulong)foundConfiguration.ChannelId);
            if (foundChannel is ISocketMessageChannel sktChannel)
            {
                sktChannel.SendMessageAsync($"User {user.Username} has left the guild, they were a member since {user.JoinedAt}.");
            }

            return Task.CompletedTask;
        }

        private Task EventRouterOnUserJoined(SocketGuildUser user)
        {
            var foundConfiguration = Configuration.ConfigurationHandler.Instance.GetConfiguration(user.Guild.Id);
            if (foundConfiguration == null) return Task.CompletedTask;

            var foundChannel = user.Guild.GetChannel((ulong)foundConfiguration.ChannelId);
            if (foundChannel is ISocketMessageChannel sktChannel)
            {
                sktChannel.SendMessageAsync($"User {user.Username} has joined the guild");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            InternalDatabase.Handler.Instance.GetConnection().DbConnection.Close();
        }

        public EventRouter EventRouter { get; set; }
    }
}