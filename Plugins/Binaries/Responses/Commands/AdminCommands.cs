using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Auditor;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Responses.Commands
{
    internal class AdminCommands
    {
        [Command("svr",
            "Switches the voice server to a random one, and back to the original one again to reconnect everyone in voice channels")]
        [Alias("switchvoiceregion")]
        public async void SwitchVoiceServer(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.Channel is SocketGuildChannel _socketGuildChannel)
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
                    var newVoiceRegion =
                        voiceRegionsWithoutCurrent.ElementAt(rand.Next(voiceRegionsWithoutCurrent.Count()));

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

        [Command("userinfo",
            "Get's otherwise invisible information about a user that the discord client cannot retrieve. USAGE: userinfo <userid>")]
        public async void GetUserInfo(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{sktMessage.Author.Username} - Invalid use of userinfo, try !help");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel _socketGuild)
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
                    embedBuilder.AddField("Created",
                        $"{user.CreatedAt.Date} ({(DateTime.Now - user.CreatedAt.Date).Days} days ago)");

                    if (user.JoinedAt != null)
                        embedBuilder.AddField("Joined This Guild",
                            $"{user.JoinedAt.Value.Date} ({(DateTime.Now - user.JoinedAt.Value.Date).Days} days ago)");

                    embedBuilder.AddField("Current Nickname", user.Nickname ?? "None");
                    embedBuilder.AddField("Avatar Identifier", user.AvatarId);
                    embedBuilder.AddField("Is Deafened", user.IsDeafened);
                    embedBuilder.AddField("Is Muted", user.IsMuted);
                    embedBuilder.AddField("Is A Bot", user.IsBot);
                    embedBuilder.AddField("Is Suppressed", user.IsSuppressed);

                    await sktMessage.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
        }

        [Command("channelinfo",
            "Gets the channels info, as well as the current Guild ID that the channel belongs to - execute within the channel you wish to get the info for.")]
        public async void GetChannelInfo(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var _guildID = "UNKNOWN";
            if (sktMessage.Channel is SocketGuildChannel _socketGuild) _guildID = _socketGuild.Guild.Id.ToString();

            await sktMessage.Channel.SendMessageAsync(
                $"Channel ID is {sktMessage.Channel.Id} which is in the guild {_guildID}");
        }

        [Command("rmiu", "Removes inactive users from the guild (!rmiu <days> [roleid to keep] [..] [..] [..])")]
        public async void RemoveInactiveUsers(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var db = InternalDatabase.Handler.Instance.GetConnection("Auditor");
            if (db == null)
            {
                await sktMessage.Channel.SendMessageAsync("Unable to connect to the Auditor Database, try again later");
                return;
            }

            if (int.TryParse(parameters[1], out var dayCount))
            {
                var isDryRun = false;
                var dt = DateTime.Now.AddDays(dayCount * -1);
                var dryRunMessage = $"Parameters: {String.Join(",",parameters)}\r\nTimestamp: {dt.ToUniversalTime().Ticks}\r\n\r\n";
                if (parameters.Length >= 3)
                {
                    if (parameters[2].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        isDryRun = true;
                    }
                }

                var skipRoles = new List<ulong>();
                for (var i = 2; i < parameters.Length; i++)
                {
                    if ( ulong.TryParse(parameters[i], out var ulongResult ) )
                        skipRoles.Add(ulongResult);
                }

                var tableMapping = db.DbConnection.TableMappings.DefaultIfEmpty(null).FirstOrDefault(x => x.TableName.Equals("AuditEntry"));
                if (tableMapping == null)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        "Unable to locate the AuditEntry database table, something has gone weirdly wrong...");
                    return;
                }

                if (sktMessage.Channel is SocketGuildChannel _channel)
                {
                    var kickedUserCount = 0;
                    var currentIteration = 0;
                    var totalUsers = _channel.Guild.Users.Count;

                    await sktMessage.Channel.SendMessageAsync(
                        $"Attempting to find users who have not been active in {dayCount} days... This will take awhile! IsDryRun:{isDryRun}");

                    foreach (var user in _channel.Guild.Users.Where(x => !x.IsBot && !x.Roles.Any(y => y.Permissions.Administrator)))
                    {
                        currentIteration++;
                        var percentCompletion = (int)Math.Round((double)currentIteration * 100 / totalUsers);
                        
                        if (percentCompletion % 10 == 0)
                        {
                            await sktMessage.Channel.SendMessageAsync(
                                $"Current Task Completion: {percentCompletion}% ({currentIteration}/{totalUsers})");
                        }

                        if (skipRoles.Any())
                        {
                            if (user.Roles.Any(role => skipRoles.Any(zz => zz.Equals(role.Id))))
                            {
                                dryRunMessage = $"{dryRunMessage}[SKIP] {user.Username} skipped as they belong to a skipped role\r\n";
                                continue;
                            }
                        }

                        var queryResults = db.DbConnection.Query(tableMapping, "SELECT * FROM AuditEntry WHERE Type = 2 AND UserId = ? AND GuildId = ? AND Timestamp > ?", new object[] { (long)user.Id, (long)user.Guild.Id, (long)dt.ToUniversalTime().Ticks });
                        if (queryResults.Count == 0)
                        {
                            if (isDryRun)
                            {
                                dryRunMessage = $"{dryRunMessage}[KICK] {user.Username} would have been kicked for inactivity.\r\n";
                            }
                            else
                            {
                                await user.SendMessageAsync(
                                    $"You were kicked by AGNSharpBot: 'You have not been active within this guild: {_channel.Guild.Name}, and therefore have been removed.  If you believe this was in error, you're free to rejoin at https://www.agngaming.com/discord'");
                                await user.KickAsync(
                                    $"You were kicked by AGNSharpBot: 'You have not been active within this guild: {_channel.Guild.Name}, and therefore have been removed.  If you believe this was in error, you're free to rejoin at https://www.agngaming.com/discord'");
                            }
                            kickedUserCount++;
                        }
                        else
                        {
                            var auditEntry = (AuditorSql.AuditEntry) queryResults.Last();
                            dryRunMessage = $"{dryRunMessage}[KEPT] {user.Username} last spoke {auditEntry.Timestamp}\r\n";
                        }
                    }

                    if (isDryRun)
                    {
                        await sktMessage.Channel.SendMessageAsync($"I would have kicked {kickedUserCount} users from this guild, results below.");

                        var stream = new MemoryStream();
                        var writer = new StreamWriter(stream);
                        await writer.WriteAsync(dryRunMessage);
                        await writer.FlushAsync();
                        stream.Position = 0;

                        await sktMessage.Channel.SendFileAsync(stream, "results.txt");
                    }
                    else
                    {
                        await sktMessage.Channel.SendMessageAsync($"I have kicked {kickedUserCount} users from this guild.");
                    }
                }
            }
            else
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{parameters[1]} is not a supported day offset, please supply the correct offset");
            }
        }

        [Command("grm","Guild Root Management")]
        public async void RemoveBotFromGuild(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`Guild Root Management`\r\n" +
                                                          "Try !grm help for help");
                return;
            }

            if (!sktMessage.Author.Id.Equals(316565781985099777))
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"Sorry {sktMessage.Author.Username}, you do not have the permissions to run this command.");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync(
                        "This has no help, if you do not know how to use this command already, you should not be using it at all");
                    break;

                case "rm":
                    if (parameters.Length == 3)
                    {
                        try
                        {
                            ulong.TryParse(parameters[2], out var guildID);
                            var guildToRemove = discordSocketClient.Guilds.DefaultIfEmpty(null)
                                .FirstOrDefault(x => x.Id == guildID);

                            if (guildToRemove != null)
                            {
                                var guildName = guildToRemove.Name;
                                await guildToRemove.LeaveAsync();
                                await sktMessage.Channel.SendMessageAsync($"I removed myself from {guildName}, yeet!");
                            }
                            else
                            {
                                await sktMessage.Channel.SendMessageAsync(
                                    "Unable to leave the supplied guild, as I believe I am not in it");
                            }
                        }
                        catch (Exception)
                        {
                            await sktMessage.Channel.SendMessageAsync($"Could not convert {parameters[2]} to a ulong");
                        }
                    }
                    else
                    {
                        await sktMessage.Channel.SendMessageAsync("Missing parameter");
                    }
                    break;

                case "list":
                    var guildList = $"I am in {discordSocketClient.Guilds.Count} guilds, here is the list:\r\n\r\n";
                    foreach (var guild in discordSocketClient.Guilds)
                    {
                        guildList += $"{guild.Name} ({guild.Id} | {guild.MemberCount})\r\n";
                    }

                    await sktMessage.Channel.SendMessageAsync(guildList);
                    break;

                default:
                    break;
            }

            //discordSocketClient.Guilds.First().LeaveAsync()
        }

        /*
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
        */
    }
}