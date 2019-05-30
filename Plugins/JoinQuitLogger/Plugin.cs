using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using PluginInterface;
using Discord.WebSocket;
using GlobalLogger;
using GlobalLogger.AdvancedLogger;
using Logger = GlobalLogger.Logger;

namespace JoinQuitLogger
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "JoinQuitLogger";
        public DiscordSocketClient DiscordClient { get; set; }

        void IPlugin.ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() {Compress = true});
            AdvancedLoggerHandler.Instance.GetLogger().Log($"JoinQuitLogger.dll Plugin Loading...");
            Config.ConfigurationHandler.Instance.Init();
            DiscordClient.UserJoined += OnUserJoinedGuild;
            DiscordClient.UserLeft += OnUserLeftGuild;
        }

        private async Task OnUserLeftGuild(SocketGuildUser socketGuildUser)
        {
            await Logger.Instance.Log(
                $"User {socketGuildUser.Username} has left this guild at {DateTime.Now}",
                Logger.LoggerType.DiscordOnly,
                new DiscordMention(0,
                    Config.ConfigurationHandler.Instance.ConfigurationRoot.JoinLeaveMessageOutput.GuildId,
                    Config.ConfigurationHandler.Instance.ConfigurationRoot.JoinLeaveMessageOutput.ChannelId));
        }

        private async Task OnUserJoinedGuild(SocketGuildUser socketGuildUser)
        {
            var taskResult = Config.ConfigurationHandler.Instance.UserJoined(socketGuildUser);
            var joinedUser = taskResult.Result;

            var joinedMessage = "";
            joinedMessage = joinedUser.TimesJoined.Equals(1) ? 
                $"User {socketGuildUser.Username} has joined this guild on {joinedUser.JoinedDate.Value}." : 
                $"User {socketGuildUser.Username} has joined this guild on {joinedUser.JoinedDate.Value}, they have joined this guild {joinedUser.TimesJoined} times before.";

            await Logger.Instance.Log(
                joinedMessage,
                Logger.LoggerType.DiscordOnly,
                new DiscordMention(0,
                    Config.ConfigurationHandler.Instance.ConfigurationRoot.JoinLeaveMessageOutput.GuildId,
                    Config.ConfigurationHandler.Instance.ConfigurationRoot.JoinLeaveMessageOutput.ChannelId));
        }

        void IPlugin.Dispose()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("JoinQuitLogger Disposed");
        }
    }
}
