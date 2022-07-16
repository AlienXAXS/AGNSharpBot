using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using Interface;
using PluginManager;

namespace VoiceChannelRoles
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "VoiceChannelRoles";
        public string Description => "Allows the adding and removing of roles based on a users current voice channel.";
        public EventRouter EventRouter { get; set; }
        public void ExecutePlugin()
        {
            var db = InternalDatabase.Handler.Instance.NewConnection();
            db.RegisterTable<SQLite.Tables.Channels>();

            EventRouter.UserVoiceStateUpdated += EventRouterOnUserVoiceStateUpdated;
            HandlerManager.Instance.RegisterHandler<Commands.LinkHandler>();
        }

        private Task EventRouterOnUserVoiceStateUpdated(SocketUser user, SocketVoiceState previousState, SocketVoiceState newState)
        {
            if (previousState.VoiceChannel != null && newState.VoiceChannel != null &&
                previousState.VoiceChannel.Id.Equals(newState.VoiceChannel.Id)) return Task.CompletedTask;

            if (user is SocketGuildUser _sktGuildUser)
            {
                if (previousState.VoiceChannel == null && newState.VoiceChannel != null)
                {
                    AddRoleToUser(_sktGuildUser, newState);
                } else if (previousState.VoiceChannel != null && newState.VoiceChannel == null)
                {
                    RemoveRoleFromUser(_sktGuildUser, previousState);
                } else if (previousState.VoiceChannel != null && newState.VoiceChannel != null)
                {
                    // User moved channel, remove old role add new
                    RemoveRoleFromUser(_sktGuildUser, previousState);
                    AddRoleToUser(_sktGuildUser, newState);
                }
            }

            return Task.CompletedTask;
        }

        private Task RemoveRoleFromUser(SocketGuildUser sktGuildUser, SocketVoiceState voiceState)
        {
            // Remove their role
            var voiceChannel = voiceState.VoiceChannel;
            if (SQLite.SQLiteHandler.Check(voiceChannel.Guild.Id, voiceChannel.Id))
            {
                var assignRoleId = SQLite.SQLiteHandler.GetRoleId(voiceChannel.Guild.Id, voiceChannel.Id);

                if (sktGuildUser.Roles.Any(x => x.Id.Equals(assignRoleId)))
                {
                    sktGuildUser.RemoveRoleAsync(assignRoleId, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).Wait();
                }
            }

            return Task.CompletedTask;
        }

        private Task AddRoleToUser(SocketGuildUser sktGuildUser, SocketVoiceState voiceState)
        {
            // Add the user to the role
            var voiceChannel = voiceState.VoiceChannel;
            if (SQLite.SQLiteHandler.Check(voiceChannel.Guild.Id, voiceChannel.Id))
            {
                var assignRoleId = SQLite.SQLiteHandler.GetRoleId(voiceChannel.Guild.Id, voiceChannel.Id);
                var foundRole = sktGuildUser.Roles.Any(x => x.Id.Equals(assignRoleId));
                if (!foundRole)
                {
                    sktGuildUser.AddRoleAsync(assignRoleId, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).Wait();
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            EventRouter.UserVoiceStateUpdated -= EventRouterOnUserVoiceStateUpdated;
        }
    }
}