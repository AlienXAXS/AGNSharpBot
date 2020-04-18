using CommandHandler;
using Discord.WebSocket;
using Responses.Commands.Handlers;
using System.Collections.Generic;
using System.Linq;

namespace Responses.Commands
{
    internal class AuthorisedCommands
    {
        private readonly AuthorisedCommandsPermission _authCmdsPermissonsHandler = new AuthorisedCommandsPermission();

        [Command("move", "Moves a user to your voice channel, mention a user to move them - Usage: !move <username> [username] [username]...")]
        public async void MoveMember(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.MentionedUsers.Count == 0)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{sktMessage.Author.Username} - Wrong syntax, please @mention the user(s) you want to move");
                return;
            }

            var CollectedUsers = new List<SocketGuildUser>();
            var sktAuthorUser = (SocketGuildUser)sktMessage.Author;

            if (sktAuthorUser.VoiceChannel == null)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{sktAuthorUser.Username} - Unable to move the requested user as you are not in a voice channel, join one first and try again");
                return;
            }

            if (_authCmdsPermissonsHandler.UserHasPermission("move", sktMessage))
            {
                foreach (var user in sktMessage.MentionedUsers)
                {
                    if (!(user is SocketGuildUser sktUser))
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Unable to move {user.Username} as I am unable to find this user.");
                        continue;
                    }

                    // If their trying to move an admin, and they are not an admin deny this access.
                    if (sktUser.Roles.Any(x => x.Permissions.Administrator) &&
                        !sktAuthorUser.Roles.Any(x => x.Permissions.Administrator))
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Unable to move {sktUser.Username}, You cannot move Administrators");
                        return;
                    }

                    if (sktUser.VoiceChannel == null)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Unable to move {sktUser.Username}, this user is not in a voice channel.  Please have them connect to a voice channel first, then retry the command");
                        return;
                    }
                    else
                    {
                        if (sktUser.VoiceChannel == sktAuthorUser.VoiceChannel)
                        {
                            await sktMessage.Channel.SendMessageAsync(
                                $"Unable to move {sktUser.Username}, this user is already with you.");
                            return;
                        }
                    }

                    await sktUser.ModifyAsync(properties => properties.Channel = sktAuthorUser.VoiceChannel);
                }

                await sktMessage.Channel.SendMessageAsync("User(s) have been moved to your voice channel");
            }
            else
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{sktMessage.Author.Username} - You do not have permission to run this command");
            }
        }
    }
}