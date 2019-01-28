using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;
using PermissionHandler;
using PermissionHandler.DB;

namespace Responses.Commands
{
    class AdminPermissions
    {
        [Command("perm", "Manages the permission system (try !perm help)")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void AdminPermission(string[] parameters, SocketMessage sktMessage,
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
                    Help(parameters, sktMessage, discordSocketClient);
                    break;

                case "listpaths":
                    ListPaths(parameters, sktMessage, discordSocketClient);
                    break;

                case "adduid":
                case "addrid":
                    NotReadyCommand(parameters, sktMessage, discordSocketClient);
                    break;

                case "add":
                    Add(parameters, sktMessage, discordSocketClient);
                    break;

                case "remove":
                    Remove(parameters, sktMessage, discordSocketClient);
                    break;
            }
        }

        private void Remove(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            // Integrity check against length of params, expecting at least 4
            // will do deny check using last param, check if boolean.
            if (parameters.Length < 4 || (sktMessage.MentionedRoles.Count == 0 && sktMessage.MentionedUsers.Count == 0))
            {
                SendMessage("Invalid use of add - Expected parameters !perm remove @member/@role", sktMessage);
                return;
            }

            var path = parameters[2];

            try
            {
                Permission.Instance.Remove(path, (sktMessage.MentionedUsers.Count == 0
                    ? sktMessage.MentionedRoles.First().Id
                    : sktMessage.MentionedUsers.First().Id));

                SendMessage($"Permission has been set successfully", sktMessage);
            }
            catch (Exception ex)
            {
                SendMessage($"Unable to remove the permission.\r\n\r\n`Error Details`\r\n{ex.Message}", sktMessage);
            }
        }

        private async void Help(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            SendMessage("\r\n`Permissions System Help`\r\n" +
                      "AGN Sharp Bot has a new Permission system, it's in Alpha stage and could be buggy, below is how to use it - please note that any Discord role that has the Administrator permission automatically has access to every permission.\r\n\r\n" +
                      "`!perm help`\r\nThis help\r\n\r\n" +
                      "`!perm add PATH @member/@role [deny]`\r\nAdds the mentioned member or role to the permission path, alternatively specify a deny tag at the end if you want to explicitly deny access to this permission\r\n\r\n" +
                      "`!perm adduid PATH userid [deny]`\r\nAdds the given member id to the permission path, alternatively specify a deny tag at the end if you want to explicitly deny access to this permission\r\n\r\n" +
                      "`!perm addrid PATH roleid [deny]`\r\nAdds the given role id to the permission path, alternatively specify a deny tag at the end if you want to explicitly deny access to this permission\r\n\r\n" +
                      "`!perm remove PATH @member/@role`\r\nRemoves the mentioned member or role permission from the database\r\n\r\n" +
                      "`!perm listpaths`\r\nLists the current registered permission path nodes", sktMessage);
        }

        private async void ListPaths(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var output = "`Current registered permission paths`\r\n";
            foreach (var path in Permission.Instance.GetRegisteredPermissionPaths())
            {
                output += $"{path}\r\n";
            }

            SendMessage(output, sktMessage);
        }

        private async void Add(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            // Integrity check against length of params, expecting at least 4
            // will do deny check using last param, check if boolean.
            if (parameters.Length < 4 || (sktMessage.MentionedRoles.Count == 0 && sktMessage.MentionedUsers.Count == 0))
            {
                SendMessage("Invalid use of add - Expected parameters !perm add @member/@role [deny]", sktMessage);
                return;
            }

            var path = parameters[2];
            var mention = parameters[3];

            // Check if the last param was a true/false for explicit deny
            bool explicitDeny = parameters[parameters.Length-1].Equals("true", StringComparison.OrdinalIgnoreCase);

            // Will catch any raised exception from the Permissions system here.
            try
            {
                Permission.Instance.Add(path,
                    (sktMessage.MentionedUsers.Count == 0
                        ? sktMessage.MentionedRoles.First().Id
                        : sktMessage.MentionedUsers.First().Id),
                    (explicitDeny 
                        ? NodePermission.Deny 
                        : NodePermission.Allow),
                    (sktMessage.MentionedUsers.Count == 0 ? OwnerType.Role : OwnerType.User));
                SendMessage($"Permission has been set successfully", sktMessage);
            }
            catch (Exception ex)
            {
                SendMessage($"Unable to set the permission.\r\n\r\n`Error Details`\r\n{ex.Message}", sktMessage);
            }
        }

        private async void LookupRole(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.MentionedRoles.Count == 0)
            {
                SendMessage("Please mention an role to look it up", sktMessage);
                return;
            }

            var mentionedRole = sktMessage.MentionedRoles.First();

            SendMessage("Role Information:\r\n" +
                $"Role Name: {mentionedRole.Name}\r\n" +
                $"Role ID: {mentionedRole.Id}", sktMessage);
        }

        private async void NotReadyCommand(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            SendMessage("Sorry, but this command is not available at this time.", sktMessage);
        }

        private async void SendMessage(string msg, SocketMessage sktMessage)
        {
            await sktMessage.Channel.SendMessageAsync($"\r\n`Permission System`\r\n{msg}");
        }
    }
}
