using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PermissionHandler;

namespace CommandHandler
{
    public class HandlerManager
    {
        private class HandlerType
        {
            public Assembly Assembly;
            public Type Type;
        }

        private static HandlerManager _instance;
        public static HandlerManager Instance = _instance ?? (_instance = new HandlerManager());

        private DiscordSocketClient _discordSocketClient;

        private readonly List<HandlerType> _registeredHandlers = new List<HandlerType>();

        public void RegisterHandler<T>()
        {
            // Register our handler
            _registeredHandlers.Add( new HandlerType() { Assembly = Assembly.GetCallingAssembly(), Type = typeof(T) });

            // Register it with the permission system
            Permission.Instance.RegisterPermission<T>(Assembly.GetCallingAssembly());
        }

        public void RegisterDiscord(DiscordSocketClient discordSocketClient)
        {
            _discordSocketClient = discordSocketClient;
            _discordSocketClient.MessageReceived += DiscordSocketClientOnMessageReceived;
        }

        private Task DiscordSocketClientOnMessageReceived(SocketMessage socketMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(socketMessage is SocketUserMessage message)) return Task.CompletedTask;
            if (message.Source != MessageSource.User) return Task.CompletedTask;

            var commandArray = SplitArguments(socketMessage.Content);

            if (message.Content.Equals(""))
            {
                return Task.CompletedTask;
            }

            Parse(commandArray, socketMessage);

            return Task.CompletedTask;
        }

        public void Parse(string[] parameters, SocketMessage socketMessage)
        {
            // Param layout:
            /*
             * 0 = param name (game2role)
             * 1 = param command
             * 2 = param args
             * 3... = more args?
             */
            
            var paramCommand = parameters[0].Replace("!","");

            if (paramCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                socketMessage.Channel.SendMessageAsync(
                    "This command is not yet working, but soon it will dynamically load all modules and compile a help section for you.");
                return;
            }

            try
            {
                foreach (var handler in _registeredHandlers)
                {
                    //var namespaceClasses = handler.Assembly.GetTypes().Where(x =>
                    //    x.Namespace != null && x.Namespace.Equals(handler.Type.Namespace, StringComparison.Ordinal));

                    //foreach (var thisClass in namespaceClasses)
                    //{
                        //var thisClassMethods = thisClass.GetMethods();
                        foreach (var thisMethod in handler.Type.GetMethods())
                        {
                            var cmdString = (Command) thisMethod.GetCustomAttributes(typeof(Command), true).FirstOrDefault();
                            var cmdAliases = (Alias) thisMethod.GetCustomAttributes(typeof(Alias), true).FirstOrDefault();

                            var cmdPermissions = (Permissions) thisMethod.GetCustomAttributes(typeof(Permissions), true).FirstOrDefault();

                            var cmdMatch = false;
                            if (cmdString?.Value == paramCommand)
                                cmdMatch = true;
                            else
                                if ( cmdAliases != null )
                                    if (cmdAliases.Value.Any(aliasEntry => aliasEntry.Equals(paramCommand)))
                                        cmdMatch = true;

                            //NRE Check
                            if (!cmdMatch) continue;

                            if (Permission.Instance.CheckPermission((SocketGuildUser)socketMessage.Author, $"{handler.Type}.{thisMethod.Name}"))
                            {
                                // Execute the method
                                var paramArray = new object[] {parameters, socketMessage, _discordSocketClient};
                                var activator = Activator.CreateInstance(handler.Type);
                                thisMethod.Invoke(activator, paramArray);
                            }
                            else
                            {
                                socketMessage.Channel.SendMessageAsync(
                                    $"{socketMessage.Author.Username}, You do not have the permissions to run that command");
                            }
                        }
                    //}
                }
            }
            catch (Exception ex)
            {
                socketMessage.Channel.SendMessageAsync(ex.Message);
                socketMessage.Channel.SendMessageAsync(ex.StackTrace);
            }
        }

        private bool CheckPermissions(Permissions.PermissionTypes permission, SocketMessage socketMessage)
        {
            switch (permission)
            {
                case Permissions.PermissionTypes.Administrator:
                    return ((SocketGuildUser)socketMessage.Author).Roles.Any(x => x.Permissions.Administrator);

                case Permissions.PermissionTypes.Guest:
                    return true;
            }

            return false;
        }

        private string[] SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
