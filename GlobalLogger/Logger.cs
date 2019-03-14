using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace GlobalLogger
{
    public class DiscordMention
    {
        public ulong UserId;
        public ulong GuildId;
        public ulong ChannelId;

        public DiscordMention(ulong userId, ulong guildId, ulong channelId)
        {
            UserId = userId;
            GuildId = guildId;
            ChannelId = channelId;
        }
    }

    /// <summary>
    /// My logger class.
    ///     (nearly) every console output goes through here first to output file name, line number, time date etc to the console for better debugging
    /// </summary>
    public class Logger
    {
        public enum LoggerType
        {
            ConsoleOnly,
            DiscordOnly,
            ConsoleAndDiscord
        }

        private static Logger _instance;
        public static Logger Instance = _instance ?? (_instance = new Logger());

        private DiscordSocketClient _discordSocketClient;

        public Logger()
        {
            //TODO: Setup file logging to %APPDATA% or similar
        }

        public DiscordMention NewDiscordMention(ulong userId, ulong guildId, ulong channelId)
        {
            return new DiscordMention(userId, guildId, channelId);
        }

        public void SetDiscordClient(DiscordSocketClient discordSocket)
        {
            _discordSocketClient = discordSocket;
            Configuration.Discord.Instance.LoadConfiguration();
        }

        public void WriteConsole(string msg, [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "", [System.Runtime.CompilerServices.CallerFilePath]
            string memberFilePath = "", [System.Runtime.CompilerServices.CallerLineNumber]
            int memberLineNumber = 0)
        {
            OutToConsole(msg, memberName, memberFilePath, memberLineNumber);
        }

        public async Task Log(string msg, LoggerType loggerType, DiscordMention discordMention = null, Embed discordEmbed = null,
            [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "", [System.Runtime.CompilerServices.CallerFilePath]
            string memberFilePath = "", [System.Runtime.CompilerServices.CallerLineNumber]
            int memberLineNumber = 0)
        {
            switch (loggerType)
            {
                case LoggerType.ConsoleAndDiscord:
                    OutToConsole(msg, memberName, memberFilePath, memberLineNumber);
                    await OutToDiscord(msg, memberName, memberFilePath, memberLineNumber, discordEmbed, discordMention);
                    break;

                case LoggerType.ConsoleOnly:
                    OutToConsole(msg, memberName, memberFilePath, memberLineNumber);
                    break;

                case LoggerType.DiscordOnly:
                    await OutToDiscord(msg, memberName, memberFilePath, memberLineNumber, discordEmbed, discordMention);
                    break;
            }
        }

        private void OutToConsole(string msg, string memberName, string memberFilePath, int memberLineNumber)
        {
            Console.WriteLine($"{DateTime.Now:g} [{memberName}|{System.IO.Path.GetFileName(memberFilePath)}|{memberLineNumber}] - {msg}");
        }

        public void LogDiscordUserMessageToFile(SocketGuildUser user, SocketMessage message)
        {
            if (!System.IO.Directory.Exists("ChatLogs"))
                System.IO.Directory.CreateDirectory("ChatLogs");

            var filePath = $@".\ChatLogs\{message.Channel.Id}.log";
            System.IO.File.AppendAllText(filePath, $"[{DateTime.Now} @ {message.Channel.Name}] {user.Username}: {message.Content}{Environment.NewLine}");

            Console.WriteLine($"{message.Channel.Name} - {user.Username}: {message.Content}");
        }

        private async Task OutToDiscord(string msg, string memberName, string memberFilePath, int memberLineNumber,
            Embed discordEmbed = null, DiscordMention discordMention = null)
        {
            // If we're not setup for discord messages, do not process this
            if (Configuration.Discord.Instance.GetDiscordLoggerChannelId() == 0 || _discordSocketClient == null || Configuration.Discord.Instance.GetDiscordGuildId() == 0) return;

            var discordGuild = _discordSocketClient.GetGuild(discordMention?.GuildId ?? Configuration.Discord.Instance.GetDiscordGuildId());

            if (discordGuild.GetChannel(discordMention?.ChannelId ?? Configuration.Discord.Instance.GetDiscordLoggerChannelId()) is ISocketMessageChannel discordChannel)
            {
                if ( discordEmbed != null )
                    await discordChannel.SendMessageAsync("", false, discordEmbed);
                else
                    await discordChannel.SendMessageAsync(discordMention?.UserId > 0 ? $"(<@{discordMention.UserId}>) | {msg}" : msg);
            }
        }
    }
}
