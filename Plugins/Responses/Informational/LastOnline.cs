using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using Responses.SQLTables;

namespace Responses.Informational
{
    class LastOnline
    {
        [Command("lo",
            "lo <@user> - Returns when a user was last seen online, you can pass multiple users into this command.")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        [Alias("lastonline", "online")]
        public async void CheckLastOnline(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var sqlDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                .Table<SQLTables.LastOnlineTable>();

            // We have a mention, use that ID
            if (sktMessage.MentionedUsers.Count > 0)
            {

                var outputMessage = "";
                foreach (var user in sktMessage.MentionedUsers)
                {
                    switch (user.Status)
                    {
                        case UserStatus.Offline:
                            var foundUser = sqlDb.DefaultIfEmpty(null)
                                .FirstOrDefault(x => x != null && x.DiscordId.Equals((long) user.Id));
                            if (foundUser == null)
                                outputMessage +=
                                    $"User {user.Username} cannot be found in my database, I've just not seen them online yet\r\n";
                            else
                            {
                                var timeElapsed = DateTime.Now - foundUser.DateTime;
                                outputMessage +=
                                    $"User {user.Username} was last seen online at {foundUser.DateTime} ({Util.ReadableTimespan.GetReadableTimespan(timeElapsed)} ago)\r\n";
                            }

                            break;

                        case UserStatus.Invisible:
                            outputMessage += $"User {user.Username} is online, but is invisible\r\n";
                            break;

                        case UserStatus.Online:
                        case UserStatus.AFK:
                        case UserStatus.DoNotDisturb:
                        case UserStatus.Idle:
                            outputMessage += $"User {user.Username} is online right now!\r\n";
                            break;
                    }
                }

                await sktMessage.Channel.SendMessageAsync(outputMessage);
            }
            else
            {
                if (sktMessage.Content.ToLower().Contains("mccann"))
                {
                    await sktMessage.Channel.SendMessageAsync(
                        "Fuck knows where she is, the parents probs did it though...");
                }
                else
                    await sktMessage.Channel.SendMessageAsync(
                        "Please mention a user to discover when they were last online, such as: !lo @Username");
            }
        }
    }
}