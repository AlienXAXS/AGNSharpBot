using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using DiscordMenu;

namespace Responses.Commands
{
    class AdminCommands
    {
        [Command("svr", "Switches the voice server to a random one, and back to the original one again to reconnect everyone in voice channels")]
        [Alias("switchvoiceregion")]
        public async void SwitchVoiceServer(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.Channel is SocketGuildChannel _socketGuildChannel)
            {
                try
                {
                    var voiceRegions = await _socketGuildChannel.Guild.GetVoiceRegionsAsync();
                    var currentVoiceRegionId = _socketGuildChannel.Guild.VoiceRegionId;

                    var currentVoiceRegion = voiceRegions.First(x => x.Id.Equals(currentVoiceRegionId));

                    var voiceRegionsWithoutCurrent = voiceRegions.Where(x => x.Id != currentVoiceRegionId);

                    await sktMessage.Channel.SendMessageAsync(
                        $"Switch Voice Server Initiated, Current voice region: {currentVoiceRegion.Name} [{currentVoiceRegionId}]");
                    await sktMessage.Channel.SendMessageAsync("Switching to a random Voice Region, please wait...");

                    var rand = new Random(DateTime.Now.Millisecond);
                    var newVoiceRegion = voiceRegionsWithoutCurrent.ElementAt(rand.Next(voiceRegionsWithoutCurrent.Count()));

                    await _socketGuildChannel.Guild.ModifyAsync(properties =>
                        properties.Region = newVoiceRegion);

                    await sktMessage.Channel.SendMessageAsync(
                        $"Voice region is now {newVoiceRegion.Name}, switching back to previous");

                    await _socketGuildChannel.Guild.ModifyAsync(properties =>
                        properties.Region = currentVoiceRegion);

                    await sktMessage.Channel.SendMessageAsync("Voice Region switch complete!");
                }
                catch (Exception ex)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        $"Unable to switch the voice region, the error is as follows:\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                }
            }
        }

        [Command("userinfo", "Get's otherwise invisible information about a user that the discord client cannot retrieve. USAGE: userinfo <userid>")]
        public async void GetUserInfo(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync($"{sktMessage.Author.Username} - Invalid use of userinfo, try !help");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel _socketGuild)
            {
                if (ulong.TryParse(parameters[1], out var userId))
                {
                    // UID based search
                    var user = _socketGuild.Users.Where(x => x.Id == userId).DefaultIfEmpty(null).FirstOrDefault();
                    if (user == null)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            "I am unable to find a user with that ID");
                        return;
                    }

                    var embedBuilder = new EmbedBuilder();
                    embedBuilder.Title = $"User information for {user.Username}";
                    embedBuilder.AddField("Created", $"{user.CreatedAt.Date} ({(DateTime.Now - user.CreatedAt.Date).Days} days ago)");

                    if (user.JoinedAt != null)
                    {
                        embedBuilder.AddField("Joined This Guild",
                            $"{user.JoinedAt.Value.Date} ({(DateTime.Now - user.JoinedAt.Value.Date).Days} days ago)");
                    }


                    embedBuilder.AddField("Current Nickname", user.Nickname ?? "None");
                    embedBuilder.AddField("Avatar Identifier", user.AvatarId);
                    embedBuilder.AddField("Is Deafened", user.IsDeafened);
                    embedBuilder.AddField("Is Muted", user.IsMuted);
                    embedBuilder.AddField("Is A Bot", user.IsBot);
                    embedBuilder.AddField("Is Suppressed", user.IsSuppressed);

                    await sktMessage.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
                else
                {
                    //todo
                }
            }
        }

        [Command("channelinfo", "Gets the channels info, as well as the current Guild ID that the channel belongs to - execute within the channel you wish to get the info for.")]
        public async void GetChannelInfo(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {

            var _guildID = "UNKNOWN";
            if (sktMessage.Channel is SocketGuildChannel _socketGuild)
            {
                _guildID = _socketGuild.Guild.Id.ToString();
            }

            await sktMessage.Channel.SendMessageAsync($"Channel ID is {sktMessage.Channel.Id} which is in the guild {_guildID}");
        }

        [Command("rmmsg", "rmmsg <userid> <#messages> [from msg id] - Removes the specified number of messages for a user in the channel you execute the command in")]
        public async void RemoveUserMessages(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length < 3)
            {
                await sktMessage.Channel.SendMessageAsync("Invalid parameters supplied, try !help");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel socketGuild)
            {
                if (ulong.TryParse(parameters[1], out var userId))
                {
                    if (int.TryParse(parameters[2], out var numMessages))
                    {
                        if (numMessages <= 30)
                        {
                            // UID based search
                            var user = socketGuild.Users.Where(x => x.Id == userId).DefaultIfEmpty(null).FirstOrDefault();
                            if (user == null)
                            {
                                await sktMessage.Channel.SendMessageAsync("I am unable to find a user with that ID");
                                return;
                            }

                            var deletedMessageCount = 0;
                            List<IMessage> sortedList = sortedList = await sktMessage.Channel.GetMessagesAsync()?.Flatten().Where(x => x.Author.Id == userId).ToList();

                            // Delete from a message
                            if (parameters.Length == 4 && ulong.TryParse(parameters[3], out var startMessageId))
                            {
                                if (sortedList.All(x => x.Id != startMessageId))
                                {
                                    await sktMessage.Channel.SendMessageAsync(
                                        $"Unable to find message from {user.Username} with id {startMessageId}");
                                    return;
                                }

                                sortedList = sortedList.GetRange(sortedList.FindIndex(x => x.Id == startMessageId),
                                    numMessages);
                            }

                            // If we have no results, there are no messages.
                            if (sortedList == null)
                            {
                                await sktMessage.Channel.SendMessageAsync(
                                    "Unable to find any messages that match your filters");
                                return;
                            }

                            sortedList.Sort((x,y) => y.CreatedAt.CompareTo(x.CreatedAt));

                            var statusMessage = await sktMessage.Channel.SendMessageAsync(
                                $"Attempting to delete {numMessages} of {user.Username}'s messages from {sktMessage.Channel.Name}, please wait...");

                            var i = 0;
                            foreach (var cachedMessage in sortedList)
                            {
                                if (i == numMessages) break;

                                deletedMessageCount++;
                                await cachedMessage.DeleteAsync();
                                i++;
                            }

                            await statusMessage.ModifyAsync(properties => properties.Content = $"Successfully deleted {deletedMessageCount} of {user.Username}'s messages in this channels message cache");
                        }
                        else
                        {
                            await sktMessage.Channel.SendMessageAsync(
                                "Invalid parameters supplied, number of messages out of range: 1 -> 30");
                        }
                    }
                    else
                    {
                        await sktMessage.Channel.SendMessageAsync("Invalid parameters supplied, try !help");
                    }
                }
                else
                {
                    await sktMessage.Channel.SendMessageAsync("Invalid parameters supplied, try !help");
                }
            }
        }
    }
}
