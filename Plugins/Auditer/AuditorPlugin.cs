using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using PluginInterface;

namespace Auditor
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "Auditor";
        public DiscordSocketClient DiscordClient { get; set; }

        public void ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true).SetRetentionOptions(new RetentionOptions(){Compress = true, Days = 1});

            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<AuditorSQL>();

            DiscordClient.MessageDeleted += DiscordClientOnMessageDeleted;
            DiscordClient.MessageUpdated += DiscordClientOnMessageUpdated;
            DiscordClient.MessageReceived += DiscordClientOnMessageReceived;
            DiscordClient.GuildMemberUpdated += DiscordClientOnGuildMemberUpdated;
            DiscordClient.UserJoined += DiscordClientOnUserJoined;
            DiscordClient.UserLeft += DiscordClientOnUserLeft;
        }

        private void WriteToDatabase(AuditorSQL.AuditType type, ulong channelId = 0, ulong userId = 0, string contents = null,
            string prevContents = null, ulong messageId = 0, ulong guildId = 0, string notes = null, string imgUrls = null)
        {
            var db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSQL>();
            db.Connection.Insert(new AuditorSQL()
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
                ImageUrls = imgUrls
            });
        }

        private Task DiscordClientOnUserLeft(SocketGuildUser sktGuildUser)
        {
            if (sktGuildUser.IsBot || sktGuildUser.IsWebhook) return Task.CompletedTask;

            WriteToDatabase(AuditorSQL.AuditType.QUIT_GUILD, userId: sktGuildUser.Id, notes: $"User {sktGuildUser.Username} Left Guild", guildId: sktGuildUser.Guild.Id);

            return Task.CompletedTask;
        }

        private Task DiscordClientOnUserJoined(SocketGuildUser sktGuildUser)
        {
            if (sktGuildUser.IsBot || sktGuildUser.IsWebhook) return Task.CompletedTask;

            WriteToDatabase(AuditorSQL.AuditType.JOIN_GUILD, userId: sktGuildUser.Id, notes: $"User {sktGuildUser.Username} Joined Guild", guildId: sktGuildUser.Guild.Id);

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
                WriteToDatabase(AuditorSQL.AuditType.USER_OFFLINE, userId: newGuildUser.Id, notes: $"User {newGuildUser.Username} is now offline", guildId: newGuildUser.Guild.Id);
            }
            else if (oldGuildUser.Status != UserStatus.Online)
            {
                // Must be online
                WriteToDatabase(AuditorSQL.AuditType.USER_ONLINE, userId: newGuildUser.Id, notes: $"User {newGuildUser.Username} is now online", guildId: newGuildUser.Guild.Id);
            }

            if (newGuildUser.Nickname != null)
            {
                // Nickname set
                if (oldGuildUser.Nickname == null)
                    // ReSharper disable once HeuristicUnreachableCode - code is reachable
                    WriteToDatabase(AuditorSQL.AuditType.NICKNAME_NEW, userId: newGuildUser.Id,
                        notes: $"User {newGuildUser.Username} set a nickname of: {newGuildUser.Nickname}", guildId: newGuildUser.Guild.Id);

                // Nickname updated
                if (oldGuildUser.Nickname != null && !newGuildUser.Nickname.Equals(oldGuildUser.Nickname))
                    WriteToDatabase(AuditorSQL.AuditType.NICKNAME_MODIFIED, userId: newGuildUser.Id,
                        notes:
                        $"User {newGuildUser.Username} updated their nickname from {oldGuildUser.Nickname} to {newGuildUser.Nickname}", guildId: newGuildUser.Guild.Id);
            }
            else
            {
                // Nickname removed
                if (oldGuildUser.Nickname != null)
                    WriteToDatabase(AuditorSQL.AuditType.NICKNAME_DELETED, userId: newGuildUser.Id, notes:$"User {newGuildUser.Username} removed their nickname which was {oldGuildUser.Nickname}", guildId: newGuildUser.Guild.Id);
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

            WriteToDatabase(AuditorSQL.AuditType.MESSAGE_NEW, sktMessage.Channel.Id, sktMessage.Author.Id, sktMessage.Content, notes: $"Message Received by {sktMessage.Author.Username} in channel {sktMessage.Channel.Name}", guildId: guildId, messageId: sktMessage.Id, imgUrls: imgUrls);
            return Task.CompletedTask;
        }

        private Task DiscordClientOnMessageUpdated(Cacheable<IMessage, ulong> prevMessage, SocketMessage message, ISocketMessageChannel sktChannel)
        {
            // If the message is from a bot, we dont care
            if ( message.Author.IsBot ) return Task.CompletedTask;

            ulong guildId = 0;
            if (sktChannel is SocketGuildChannel socketGuildChannel)
                guildId = socketGuildChannel.Guild.Id;

            WriteToDatabase(AuditorSQL.AuditType.MESSAGE_MODIFIED, sktChannel.Id, message.Author.Id, message.Content, prevMessage.HasValue ? prevMessage.Value.Content : "Unable to get contents", message.Id, notes: $"Message Updated by {message.Author.Username} in channel {sktChannel.Name}", guildId: guildId);

            return Task.CompletedTask;
        }

        private Task DiscordClientOnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel sktChannel)
        {
            if (!message.HasValue) return Task.CompletedTask;
            if (message.Value.Author.IsBot) return Task.CompletedTask;

            ulong guildId = 0;
            if (sktChannel is SocketGuildChannel socketGuildChannel)
                guildId = socketGuildChannel.Guild.Id;

            WriteToDatabase(AuditorSQL.AuditType.MESSAGE_DELETED, sktChannel.Id, message.Value.Id, message.Value.Content, messageId: message.Value.Id, notes: $"Message Deleted by {message.Value.Author.Username} in channel {message.Value.Channel.Name}", guildId: guildId);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            
        }
    }
}
