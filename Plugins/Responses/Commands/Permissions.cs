using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace Responses.Commands
{
    class Permissions
    {
        [Command("perm", "Manages the permission system (try !perm help)")]
        [CommandHandler.Permissions(CommandHandler.Permissions.PermissionTypes.Guest)]
        public async void MenuTest(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {

            // No params given, other than !perm itself
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`Permission System`\r\n" +
                                                          "Try !perm help for help");
                return;
            }


            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("\r\n`Permissions System Help`\r\n" +
                                                              "`!perm help`\r\nThis help\r\n\r\n" +
                                                              "`!perm add PATH @member/@role [deny]`\r\nAdds the mentioned member or role to the permission path, alternatively specify a deny tag at the end if you want to explicitly deny access to this permission\r\n\r\n" +
                                                              "`!perm remove PATH @member/@role`\r\nRemoves the mentioned member or role permission from the database\r\n\r\n" +
                                                              "`!perm listpaths`\r\nLists the current registered permission path nodes");
                    break;

                case "listpaths":
                    var output = "`Current registered permission paths`\r\n";
                    foreach (var path in PermissionHandler.Permission.Instance.GetRegisteredPermissionPaths())
                    {
                        output += $"{path}\r\n";
                    }

                    await sktMessage.Channel.SendMessageAsync(output);
                    break;

            }
        }
    }
}
