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
        [Command("lo", "lo <@user> - Returns when a user was last seen online, you can pass multiple users into this command.")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        [Alias("lastonline","online")]
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
                            var foundUser = sqlDb.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.DiscordId.Equals((long)user.Id));
                            if (foundUser == null)
                                outputMessage +=
                                    $"User {user.Username} cannot be found in my database, I've just not seen them online yet\r\n";
                            else
                            {
                                var timeElapsed = DateTime.Now - foundUser.DateTime;
                                outputMessage += $"User {user.Username} was last seen online at {foundUser.DateTime} ({GetReadableTimespan(timeElapsed)} ago)\r\n";
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
                } else 
                    await sktMessage.Channel.SendMessageAsync("Please mention a user to discover when they were last online, such as: !lo @Username");
            }
        }

        public string GetReadableTimespan(TimeSpan ts)
        {
            // formats and its cutoffs based on totalseconds
            var cutoff = new SortedList<long, string> {
                {59, "{3:S}" },
                {60, "{2:M}" },
                {60*60-1, "{2:M}, {3:S}"},
                {60*60, "{1:H}"},
                {24*60*60-1, "{1:H}, {2:M}"},
                {24*60*60, "{0:D}"},
                {Int64.MaxValue , "{0:D}, {1:H}"}
            };

            // find nearest best match
            var find = cutoff.Keys.ToList()
                .BinarySearch((long)ts.TotalSeconds);
            // negative values indicate a nearest match
            var near = find < 0 ? Math.Abs(find) - 1 : find;
            // use custom formatter to get the string
            return String.Format(
                new HMSFormatter(),
                cutoff[cutoff.Keys[near]],
                ts.Days,
                ts.Hours,
                ts.Minutes,
                ts.Seconds);
        }
    }

    public class HMSFormatter : ICustomFormatter, IFormatProvider
    {
        // list of Formats, with a P customformat for pluralization
        static Dictionary<string, string> timeformats = new Dictionary<string, string> {
            {"S", "{0:P:Seconds:Second}"},
            {"M", "{0:P:Minutes:Minute}"},
            {"H","{0:P:Hours:Hour}"},
            {"D", "{0:P:Days:Day}"}
        };

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            return String.Format(new PluralFormatter(), timeformats[format], arg);
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }

    public class PluralFormatter : ICustomFormatter, IFormatProvider
    {

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg != null)
            {
                var parts = format.Split(':'); // ["P", "Plural", "Singular"]

                if (parts[0] == "P") // correct format?
                {
                    // which index postion to use
                    int partIndex = (arg.ToString() == "1") ? 2 : 1;
                    // pick string (safe guard for array bounds) and format
                    return String.Format("{0} {1}", arg, (parts.Length > partIndex ? parts[partIndex] : ""));
                }
            }
            return String.Format(format, arg);
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }
}