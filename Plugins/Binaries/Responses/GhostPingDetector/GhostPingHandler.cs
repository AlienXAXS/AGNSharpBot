using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Responses.GhostPingDetector
{
    internal class GhostPingHandler
    {
        public async Task<Task> ClientOnMessageReceived(SocketMessage _socketMessage)
        {
            if (!(_socketMessage is SocketUserMessage message)) return Task.CompletedTask;
            if (message.Source != MessageSource.User) return Task.CompletedTask;

            if (message.Author is SocketGuildUser sktGuildUser)
            {
                // Do not fire for administrators
                if (sktGuildUser.Roles.Any(x => x.Permissions.Administrator)) return Task.CompletedTask;

                if (message.Content.Contains("||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​||||​"))
                {
                    if (message.MentionedEveryone || message.MentionedUsers.Any())
                    {
                        // We have a ghost ping.
                        await message.DeleteAsync(RequestOptions.Default);

                        if (!message.MentionedUsers.Any())
                        {
                            await message.Channel.SendMessageAsync(
                                $"Deleted a GhostPing from <@{sktGuildUser.Id}> which was sent to Everyone");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync(
                                $"Deleted a GhostPing from <@{sktGuildUser.Id}> which was sent to {string.Join(",", message.MentionedUsers.Select(x => x.Username))}");
                        }
                    }
                    else
                    {
                        await message.DeleteAsync(RequestOptions.Default);
                        await message.Channel.SendMessageAsync(
                            $"Deleted GhostMessage from <@{sktGuildUser.Id}>, Ghost Message: {message.Content.Replace("|", "")}");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
