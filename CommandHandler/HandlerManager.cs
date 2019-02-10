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

        private class MethodInfoHelper
        {
            public MethodInfo MethodInfo { get; set; }
            public Command Command { get; set; }
            public Alias Alias { get; set; }
            public Permissions Permissions { get; set; }
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

            // Compile a list of methods we can use
            var methodHelpers = new List<MethodInfoHelper>();
            foreach (var handler in _registeredHandlers)
            {
                foreach (var method in handler.Type.GetMethods())
                {
                    var cmdString = (Command) method.GetCustomAttributes(typeof(Command), true).FirstOrDefault();
                    var cmdAliases = (Alias) method.GetCustomAttributes(typeof(Alias), true).FirstOrDefault();
                    var cmdPermissions = (Permissions) method.GetCustomAttributes(typeof(Permission), true).FirstOrDefault();

                    if (cmdString != null)
                        methodHelpers.Add(new MethodInfoHelper()
                        {
                            Alias = cmdAliases,
                            Command = cmdString,
                            MethodInfo = method,
                            Permissions = cmdPermissions
                        });
                }
            }

            if (paramCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
            {

                var discordEmbedBuilder = new EmbedBuilder();
                discordEmbedBuilder.WithTitle("Kitty Cat Commands");
                discordEmbedBuilder.Color = Color.Blue;
                discordEmbedBuilder.Description = "You'll only be shown the commands that you can execute";

                foreach (var thisMethod in methodHelpers)
                {
                    if (thisMethod.Permissions?.Value == Permissions.PermissionTypes.Guest ||
                        Permission.Instance.CheckPermission((SocketGuildUser) socketMessage.Author,
                            $"!{thisMethod.MethodInfo.GetType()}.{thisMethod.MethodInfo.Name}"))
                    {
                        discordEmbedBuilder.AddField($"!{thisMethod.Command.Value}", $"{thisMethod.Command.Description}\r\n\r\n");
                    }
                }

                socketMessage.Channel.SendMessageAsync(null, false, discordEmbedBuilder.Build());
                return;
            }

            try
            {
                foreach (var thisMethod in methodHelpers)
                {
                    var cmdMatch = false;
                    if (thisMethod.Command?.Value == paramCommand)
                        cmdMatch = true;
                    else
                        if ( thisMethod.Alias != null )
                            if (thisMethod.Alias.Value.Any(aliasEntry => aliasEntry.Equals(paramCommand)))
                                cmdMatch = true;

                    //NRE Check
                    if (!cmdMatch) continue;

                    if (thisMethod.Permissions?.Value == Permissions.PermissionTypes.Guest || Permission.Instance.CheckPermission((SocketGuildUser)socketMessage.Author, $"{thisMethod.MethodInfo.GetType()}.{thisMethod.MethodInfo.Name}"))
                    {
                        // Execute the method
                        var paramArray = new object[] {parameters, socketMessage, _discordSocketClient};
                        var activator = Activator.CreateInstance(thisMethod.MethodInfo.GetType());
                        thisMethod.MethodInfo.Invoke(activator, paramArray);
                    }
                    else
                    {
                        socketMessage.Channel.SendMessageAsync(
                            $"{socketMessage.Author.Username}, You do not have the permissions to run that command");
                    }
                }
            }
            catch (Exception ex)
            {
                socketMessage.Channel.SendMessageAsync(ex.Message);
                socketMessage.Channel.SendMessageAsync(ex.StackTrace);
            }
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
