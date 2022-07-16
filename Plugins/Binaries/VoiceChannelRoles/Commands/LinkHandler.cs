using System;
using System.Linq;
using System.Reflection.Metadata;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using VoiceChannelRoles.SQLite;

namespace VoiceChannelRoles.Commands
{
    internal class LinkHandler
    {
        [Command("linkvc", "Links a voice channel to a new role")]
        public async void LinkVoiceChannelToRole(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)

        {
            switch (parameters.Length)
            {
                case 1:
                    await sktMessage.Channel.SendMessageAsync("Usage: !linkvc VoiceChannelID (attempting to link an already linked vc will remove its role link)");
                    return;

                case 2:
                    if (sktMessage.Author is SocketGuildUser _sktGuildUser)
                    {
                        if (ulong.TryParse(parameters[1], out var cId))
                        {
                            var channel = _sktGuildUser.Guild.Channels.DefaultIfEmpty(null)
                                .FirstOrDefault(x => x.Id == cId);

                            if (channel is SocketVoiceChannel _sktVoiceChannel)
                            {
                                var guildId = _sktVoiceChannel.Guild.Id;
                                var vcId = _sktVoiceChannel.Id;

                                if (SQLiteHandler.Check(guildId, vcId))
                                {
                                    try
                                    {
                                        var roleId = SQLiteHandler.GetRoleId(guildId, vcId);
                                        SQLiteHandler.Remove(guildId, vcId);

                                        var foundRole = _sktGuildUser.Guild.Roles.DefaultIfEmpty(null)
                                            .FirstOrDefault(x => x.Id == roleId);

                                        if (foundRole != null)
                                        {
                                            await foundRole.DeleteAsync(new RequestOptions()
                                                {RetryMode = RetryMode.RetryRatelimit});
                                        }

                                        await sktMessage.Channel.SendMessageAsync(
                                            $"Removed link to channel {_sktVoiceChannel.Name}.");
                                    }
                                    catch (Exception exception)
                                    {
                                        await sktMessage.Channel.SendMessageAsync(
                                            $"Something went wrong: {exception.Message}");
                                    }
                                }
                                else
                                {
                                    // Add it
                                    try
                                    {
                                        var rnd = new Random(DateTime.Now.Millisecond);
                                        var roleName = $"{rnd.Next(9999)}_{_sktVoiceChannel.Name.Replace(" ", "_")}";

                                        var createdRole = await _sktGuildUser.Guild.CreateRoleAsync(roleName);

                                        SQLiteHandler.Add(guildId, vcId, createdRole.Id);

                                        await sktMessage.Channel.SendMessageAsync(
                                            $"Channel {_sktVoiceChannel.Name} was successfully linked to role {createdRole.Name}");
                                    }
                                    catch (Exception exception)
                                    {
                                        await sktMessage.Channel.SendMessageAsync(
                                            $"Something went wrong: {exception.Message}");
                                    }
                                }
                            }
                            else
                            {
                                await sktMessage.Channel.SendMessageAsync(
                                    $"{sktMessage.Author.Username}, I am unable to find a voice channel by that ID.");
                            }
                        }
                        else
                        {
                            await sktMessage.Channel.SendMessageAsync(
                                $"{sktMessage.Author.Username}, I am unable to parse that channel ID. Ensure the channel ID is in the correct format and try again");
                        }
                    }

                    break;
            }
        }
    }
}
