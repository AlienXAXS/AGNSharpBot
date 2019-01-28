using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger;
using Newtonsoft.Json;

namespace Responses.Commands.Handlers
{
    class CommandPermission
    {
        // The command itself, such as !move
        public string Command;

        // List of roles in the server that the command can be ran by.
        public List<string> PermissionRoleNames = new List<string>();

        public CommandPermission(string command, List<string> permissionRoleNames)
        {
            Command = command;
            PermissionRoleNames = permissionRoleNames;
        }
    }

    class AuthorisedCommandsPermission
    {
        private const string ConfigurationPath = "Plugins\\Config\\AuthCommandPermissions.json";
        private List<CommandPermission> _commandPermissions = new List<CommandPermission>();

        public AuthorisedCommandsPermission()
        {
            LoadConfigAsync().Wait();
        }

        public bool UserHasPermission(string command, SocketMessage socketMessage)
        {
            if (!(socketMessage.Author is SocketGuildUser author))
                throw new Exception(
                    "Unable to bind Author from SocketAuthor to SocketGuildAuthor.  Please contact the developer to fix this");

            var permissionRoleNode = _commandPermissions.Where(x => x.Command.ToLower().Equals(command.ToLower()))
                .DefaultIfEmpty(null).FirstOrDefault();

            // Return if the user has any permissions within the list of allowed roles
            if (permissionRoleNode != null)
                return author.Roles.Any(roleNode => permissionRoleNode.PermissionRoleNames.Any(x => x.ToLower().Equals(roleNode.Name.ToLower())));

            throw new Exception($"Unable to find a matching permission node for command {command}");
        }

        public async Task LoadConfigAsync()
        {
            _commandPermissions.Clear();

            if (System.IO.File.Exists(ConfigurationPath))
            {
                try
                {
                    _commandPermissions =
                        JsonConvert.DeserializeObject<List<CommandPermission>>(
                            System.IO.File.ReadAllText(ConfigurationPath));
                }
                catch (Exception ex)
                {
                    await Logger.Instance.Log($"Unable to parse SpotifySongs.json, error was:\r\n{ex.Message}", Logger.LoggerType.ConsoleOnly);
                }
            }
            else
            {
                InitConfiguration();
            }
        }

        private void InitConfiguration()
        {
            try
            {
                var tmpList = new List<CommandPermission>
                {
                    new CommandPermission("move", new List<string>() {"AGN Member", "Staff", "Something Else"})
                };

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigurationPath));
                System.IO.File.WriteAllText(ConfigurationPath,
                    JsonConvert.SerializeObject(tmpList, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Unable to init config for AuthorisedCommandsPermission\r\n{ex.Message}",
                    Logger.LoggerType.ConsoleOnly).Wait();
            }
        }
    }
}