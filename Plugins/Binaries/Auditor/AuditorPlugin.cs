using Auditor.WebServer;
using Discord;
using Discord.WebSocket;
using Interface;
using PluginManager;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Auditor
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "Auditor";

        public string Description =>
            "Audits all actions of users within your guild, accessable and searchable via a web interface.";

        public EventRouter EventRouter { get; set; }

        public void ExecutePlugin()
        {
            // Register our SQL Tables
            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<AuditorSql.AuditEntry>().RegisterTable<AuditorSql.AuditorNancyLoginSession>();

            // Register commands
            CommandHandler.HandlerManager.Instance.RegisterHandler<ControllerCommands>();

            // Setup discord hooks
            EventRouter.MessageDeleted += DiscordClientOnMessageDeleted;
            EventRouter.MessageUpdated += DiscordClientOnMessageUpdated;
            EventRouter.MessageReceived += DiscordClientOnMessageReceived;
            EventRouter.GuildMemberUpdated += DiscordClientOnGuildMemberUpdated;
            EventRouter.UserJoined += DiscordClientOnUserJoined;
            EventRouter.UserLeft += DiscordClientOnUserLeft;

            // Start nancy
            NancyServer.Instance.DiscordSocketClient = EventRouter.GetDiscordSocketClient();
            NancyServer.Instance.Start();
        }

        private void WriteToDatabase(AuditorSql.AuditEntry.AuditType type, ulong channelId = 0, ulong userId = 0, string contents = null,
            string prevContents = null, ulong messageId = 0, ulong guildId = 0, string notes = null, string imgUrls = null, string Username = null, string Nickname = null, string ChannelName = null)
        {
            var db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql>();

            var newRecord = new AuditorSql.AuditEntry()
            {
                ChannelId = (long)channelId,
                Contents = contents,
                Timestamp = DateTime.Now,
                Type = type,
                UserId = (long)userId,
                PreviousContents = prevContents,
                MessageId = (long)messageId,
                GuildId = (long)guildId,
                Notes = notes,
                ImageUrls = imgUrls,
                Nickname = Nickname,
                ChannelName = ChannelName,
                UserName = Username,
                Id = -1
            };

            try
            {
                db.Connection.Insert(newRecord);
            } catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log($"Exception writing to database for Auditor", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception:ex);
            }
        }

        private Task DiscordClientOnUserLeft(SocketGuildUser sktGuildUser)
        {
            if (sktGuildUser.IsBot || sktGuildUser.IsWebhook) return Task.CompletedTask;

            WriteToDatabase(AuditorSql.AuditEntry.AuditType.QUIT_GUILD, userId: sktGuildUser.Id, notes: $"User {sktGuildUser.Username} Left Guild", guildId: sktGuildUser.Guild.Id, Username: sktGuildUser.Username);

            return Task.CompletedTask;
        }

        private Task DiscordClientOnUserJoined(SocketGuildUser sktGuildUser)
        {
            if (sktGuildUser.IsBot || sktGuildUser.IsWebhook) return Task.CompletedTask;

            WriteToDatabase(AuditorSql.AuditEntry.AuditType.JOIN_GUILD, userId: sktGuildUser.Id, notes: $"User {sktGuildUser.Username} Joined Guild", guildId: sktGuildUser.Guild.Id, Username: sktGuildUser.Username);

            return Task.CompletedTask;
        }

        private Task DiscordClientOnGuildMemberUpdated(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser)
        {
            if (oldGuildUser.IsBot) return Task.CompletedTask;
            if (oldGuildUser.IsWebhook) return Task.CompletedTask;

            // Online / Offline
            if (newGuildUser.Status == UserStatus.Offline)
            {
                // Is offline now
                WriteToDatabase(AuditorSql.AuditEntry.AuditType.USER_OFFLINE, userId: newGuildUser.Id, notes: $"User {newGuildUser.Username} is now offline", guildId: newGuildUser.Guild.Id, Username: newGuildUser.Username, Nickname: newGuildUser.Nickname);
            }
            else if (oldGuildUser.Status != UserStatus.Online)
            {
                // Must be online
                WriteToDatabase(AuditorSql.AuditEntry.AuditType.USER_ONLINE, userId: newGuildUser.Id, notes: $"User {newGuildUser.Username} is now online", guildId: newGuildUser.Guild.Id, Username: newGuildUser.Username, Nickname: newGuildUser.Nickname);
            }

            if (newGuildUser.Nickname != null)
            {
                // Nickname set
                if (oldGuildUser.Nickname == null)
                    // ReSharper disable once HeuristicUnreachableCode - code is reachable
                    WriteToDatabase(AuditorSql.AuditEntry.AuditType.NICKNAME_NEW, userId: newGuildUser.Id,
                        notes: $"User {newGuildUser.Username} set a nickname of: {newGuildUser.Nickname}", guildId: newGuildUser.Guild.Id, Username: newGuildUser.Username, Nickname: newGuildUser.Nickname);

                // Nickname updated
                if (oldGuildUser.Nickname != null && !newGuildUser.Nickname.Equals(oldGuildUser.Nickname))
                    WriteToDatabase(AuditorSql.AuditEntry.AuditType.NICKNAME_MODIFIED, userId: newGuildUser.Id,
                        notes:
                        $"User {newGuildUser.Username} updated their nickname from {oldGuildUser.Nickname} to {newGuildUser.Nickname}", guildId: newGuildUser.Guild.Id, Username: newGuildUser.Username, Nickname: newGuildUser.Nickname);
            }
            else
            {
                // Nickname removed
                if (oldGuildUser.Nickname != null)
                    WriteToDatabase(AuditorSql.AuditEntry.AuditType.NICKNAME_DELETED, userId: newGuildUser.Id, notes: $"User {newGuildUser.Username} removed their nickname which was {oldGuildUser.Nickname}", guildId: newGuildUser.Guild.Id, Username: newGuildUser.Username, Nickname: newGuildUser.Nickname);
            }

            return Task.CompletedTask;
        }

        private Task DiscordClientOnMessageReceived(SocketMessage sktMessage)
        {
            if (sktMessage.Author.IsBot) return Task.CompletedTask;

            ulong guildId = 0;
            if (sktMessage.Channel is SocketGuildChannel socketGuildChannel)
                guildId = socketGuildChannel.Guild.Id;

            var imgUrls = sktMessage.Attachments.Aggregate("", (current, attachment) => current + $"{attachment.Url}|");

            WriteToDatabase(AuditorSql.AuditEntry.AuditType.MESSAGE_NEW, sktMessage.Channel.Id, sktMessage.Author.Id, sktMessage.Content, notes: $"Message Received by {sktMessage.Author.Username} in channel {sktMessage.Channel.Name}", guildId: guildId, messageId: sktMessage.Id, imgUrls: imgUrls, Username: sktMessage.Author.Username, ChannelName: sktMessage.Channel.Name);
            return Task.CompletedTask;
        }

        private Task DiscordClientOnMessageUpdated(Cacheable<IMessage, ulong> prevMessage, SocketMessage message, ISocketMessageChannel sktChannel)
        {
            // If the message is from a bot, we dont care
            if (message.Author.IsBot) return Task.CompletedTask;

            ulong guildId = 0;
            if (sktChannel is SocketGuildChannel socketGuildChannel)
                guildId = socketGuildChannel.Guild.Id;

            WriteToDatabase(AuditorSql.AuditEntry.AuditType.MESSAGE_MODIFIED, sktChannel.Id, message.Author.Id, message.Content, prevMessage.HasValue ? prevMessage.Value.Content : "Unable to get contents", message.Id, notes: $"Message Updated by {message.Author.Username} in channel {sktChannel.Name}", guildId: guildId, Username: message.Author.Username, ChannelName: sktChannel.Name);

            return Task.CompletedTask;
        }

        private Task DiscordClientOnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel sktChannel)
        {
            if (!message.HasValue) return Task.CompletedTask;
            if (message.Value.Author.IsBot) return Task.CompletedTask;

            ulong guildId = 0;
            if (sktChannel is SocketGuildChannel socketGuildChannel)
                guildId = socketGuildChannel.Guild.Id;

            WriteToDatabase(AuditorSql.AuditEntry.AuditType.MESSAGE_DELETED, sktChannel.Id, message.Value.Author.Id, message.Value.Content, messageId: message.Value.Id, notes: $"Message Deleted by {message.Value.Author.Username} in channel {message.Value.Channel.Name}", guildId: guildId, Username: message.Value.Author.Username, ChannelName: sktChannel.Name);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            NancyServer.Instance.Dispose();
        }
    }
}