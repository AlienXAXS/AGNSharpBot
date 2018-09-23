using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

/*
 * My first time using refection like this, might be a bit wrong.
 */

namespace GameToRole.Admin
{
    class Handler
    {
        public SocketMessage DiscordSocket { get; set; } = null;

        private const string NameSpaceToSearch = "GameToRole.Admin.Commands";
        private DiscordSocketClient _discordSocketClient;

        public void Parse(string[] parameters, DiscordSocketClient discordSocketClient)
        {
            _discordSocketClient = discordSocketClient;

            if ( DiscordSocket == null )
            {
                throw new ArgumentNullException();;
            }

            // Param layout:
            /*
             * 0 = param name (game2role)
             * 1 = param command
             * 2 = param args
             * 3... = more args?
             */

            if (parameters.Length == 1) return;
            var paramCommand = parameters[1];

            try
            {
                var namespaceClasses = Assembly.GetExecutingAssembly().GetTypes().Where(x =>
                    x.Namespace != null && x.Namespace.Equals(NameSpaceToSearch, StringComparison.Ordinal));
                foreach (var thisClass in namespaceClasses)
                {
                    var thisClassMethods = thisClass.GetMethods();
                    foreach (var thisMethod in thisClassMethods)
                    {
                        var cmdString =
                            (Command) thisMethod.GetCustomAttributes(typeof(Command), true).FirstOrDefault();
                        var cmdPermissions =
                            (Permissions) thisMethod.GetCustomAttributes(typeof(Permissions), true).FirstOrDefault();

                        //NRE Check this bitch!
                        if (cmdString != null && cmdString.Value == paramCommand)
                        {
                            if (cmdPermissions == null || CheckPermissions(cmdPermissions.Value))
                            {
                                // Execute the method
                                var paramArray = new object[] {parameters, DiscordSocket, discordSocketClient};
                                var thisType = thisMethod.GetType();

                                var activator = Activator.CreateInstance(thisClass);
                                thisMethod.Invoke(activator, paramArray);
                            }
                            else
                            {
                                DiscordSocket.Channel.SendMessageAsync(
                                    $"{DiscordSocket.Author.Username}, You do not have the permissions to run that command");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiscordSocket.Channel.SendMessageAsync(ex.Message);
                DiscordSocket.Channel.SendMessageAsync(ex.StackTrace);
            }
        }

        private bool CheckPermissions(Permissions.PermissionTypes permission)
        {
            switch (permission)
            {
                case Permissions.PermissionTypes.Administrator:
                    return ((SocketGuildUser)DiscordSocket.Author).Roles.Any(x => x.Permissions.Administrator);

                case Permissions.PermissionTypes.Guest:
                    return true;
            }

            return false;
        }
    }
}
