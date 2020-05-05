using Discord;
using Discord.WebSocket;
using PermissionHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GlobalLogger;

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
            public Type Type { get; set; }
        }

        private static HandlerManager _instance;
        public static HandlerManager Instance = _instance ?? (_instance = new HandlerManager());

        private DiscordSocketClient _discordSocketClient;

        private readonly List<HandlerType> _registeredHandlers = new List<HandlerType>();

        public void RegisterHandler<T>()
        {
            // Register our handler
            _registeredHandlers.Add(new HandlerType() { Assembly = Assembly.GetCallingAssembly(), Type = typeof(T) });

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

        public async void Parse(string[] parameters, SocketMessage socketMessage)
        {
            // Param layout:
            /*
            * 0 = param name (game2role)
            * 1 = param command
            * 2 = param args
            * 3... = more args?
            */

            if (socketMessage.Author is SocketGuildUser sktGuildUser)
            {
                // Ensures command must start with a !
                if (!parameters[0].StartsWith("!")) return;
                var paramCommand = parameters[0].Replace("!", "");

                // Compile a list of methods we can use
                var methodHelpers = new List<MethodInfoHelper>();
                foreach (var handler in _registeredHandlers)
                {
                    foreach (var method in handler.Type.GetMethods())
                    {
                        var cmdString = (Command)method.GetCustomAttributes(typeof(Command), true).FirstOrDefault();
                        var cmdAliases = (Alias)method.GetCustomAttributes(typeof(Alias), true).FirstOrDefault();
                        var cmdPermissions =
                            (Permissions)method.GetCustomAttributes(typeof(Permissions), true).FirstOrDefault();

                        if (cmdString != null)
                            methodHelpers.Add(new MethodInfoHelper()
                            {
                                Alias = cmdAliases,
                                Command = cmdString,
                                MethodInfo = method,
                                Permissions = cmdPermissions,
                                Type = handler.Type
                            });
                    }
                }

                if (paramCommand.Equals("plugins", StringComparison.OrdinalIgnoreCase) || paramCommand.Equals("plugin", StringComparison.OrdinalIgnoreCase))
                {
                    var embed = new EmbedBuilder();
                    var plugins = PluginManager.PluginHandler.Instance.GetPlugins();
                    if (parameters.Length == 1)
                    {
                        await socketMessage.Channel.SendMessageAsync($"This command controls plugins, you can enable / disable them.  Use !plugins help for commands.");
                        return;
                    }

                    switch (parameters[1].ToLower())
                    {
                        case "help":
                            embed.Title = "AGN Sharp Bot Plugin Commands";
                            embed.AddField("help", "Shows this help.");
                            embed.AddField("list", "Lists all plugins that you can enable, and their current status.");
                            embed.AddField("enable", "Enables a plugin.  Usage: !plugin enable \"Plugin Name\"");
                            embed.AddField("disable", "Disables a plugin.  Usage: !plugin enable \"Plugin Name\"");

                            await socketMessage.Channel.SendMessageAsync(embed: embed.Build());
                            break;

                        case "list":

                            foreach (var plugin in plugins)
                            {
                                var isEnabled = PluginManager.PluginHandler.Instance.ShouldExecutePlugin(plugin.GetType().Module.Name, sktGuildUser.Guild.Id) ? "Enabled" : "Disabled";
                                embed.Title = "AGN Sharp Bot Plugins";
                                embed.AddField($"{plugin.Name} (Status: {isEnabled})", plugin.Description);
                            }

                            await socketMessage.Channel.SendMessageAsync(embed: embed.Build());

                            break;

                        case "enable":
                            await SetPluginState(parameters, socketMessage, true);
                            break;

                        case "disable":
                            await SetPluginState(parameters, socketMessage, false);
                            break;
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
                            Permission.Instance.CheckPermission((SocketGuildUser)socketMessage.Author,
                                $"{thisMethod.MethodInfo.ReflectedType?.FullName}.{thisMethod.MethodInfo.Name}"))
                        {
                            discordEmbedBuilder.AddField($"!{thisMethod.Command.Value}",
                                $"{thisMethod.Command.Description}\r\n\r\n");
                        }
                    }

                    await socketMessage.Channel.SendMessageAsync(null, false, discordEmbedBuilder.Build());
                    return;
                }

                try
                {
                    foreach (var thisMethod in methodHelpers)
                    {
                        var cmdMatch = false;
                        if (thisMethod.Command?.Value == paramCommand)
                            cmdMatch = true;
                        else if (thisMethod.Alias != null)
                            if (thisMethod.Alias.Value.Any(aliasEntry => aliasEntry.Equals(paramCommand)))
                                cmdMatch = true;

                        //NRE Check
                        if (!cmdMatch) continue;

                        bool hasExplicitPermission = false;
                        if (socketMessage.Author is SocketGuildUser messageAuthor)
                        {
                            hasExplicitPermission = Permission.Instance.CheckPermission(messageAuthor, $"{thisMethod.Type}.{thisMethod.MethodInfo.Name}");

                            if (PluginManager.PluginHandler.Instance.ShouldExecutePlugin(
                                thisMethod.MethodInfo.Module.Name,
                                messageAuthor.Guild.Id))
                            {
                                if (thisMethod.Permissions?.Value == Permissions.PermissionTypes.Guest ||
                                    hasExplicitPermission)
                                {
                                    // Execute the method
                                    var paramArray = new object[] { parameters, socketMessage, _discordSocketClient };
                                    var activator = Activator.CreateInstance(thisMethod.Type);
                                    thisMethod.MethodInfo.Invoke(activator, paramArray);
                                }
                                else
                                {
                                    await socketMessage.Channel.SendMessageAsync(
                                        $"{socketMessage.Author.Username}, You do not have the permissions to run that command");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log($"Unable to parse command \"{parameters.Aggregate((x, y) => x + " " + y)}\"",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
            }
            else
            {
                await socketMessage.Author.SendMessageAsync("Hey, thanks so much for sending me a private message - but AGNSharpBot just does not want to talk to you, go away now.... Shoo!");
            }
        }

        private async Task SetPluginState(string[] parameters, SocketMessage socketMessage, bool status)
        {
            if (parameters.Length != 3)
            {
                await socketMessage.Channel.SendMessageAsync($"Invalid parameters provided.");
                return;
            }

            var plugins = PluginManager.PluginHandler.Instance.GetPlugins();

            if (socketMessage.Author is SocketGuildUser socketGuildUser)
            {
                var pluginName = parameters[2];

                try
                {
                    PluginManager.PluginHandler.Instance.SetPluginState(pluginName, socketGuildUser.Guild.Id, status);
                    if (status)
                        await socketMessage.Channel.SendMessageAsync($"The plugin was successfully enabled.");
                    else
                        await socketMessage.Channel.SendMessageAsync($"The plugin was successfully disabled.");
                }
                catch (Exception ex)
                {
                    await socketMessage.Channel.SendMessageAsync($"Error while processing request: {ex.Message}");
                }
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