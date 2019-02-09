﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord.WebSocket;
using PluginInterface;

namespace GameToRole
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Game 2 Roles";
        List<string> IPlugin.Commands => new List<string>() { "game2role" };
        List<PluginRequestTypes.PluginRequestType> IPlugin.RequestTypes =>
            new List<PluginRequestTypes.PluginRequestType> { PluginRequestTypes.PluginRequestType.COMMAND };

        public DiscordSocketClient DiscordClient { get; set; }

        void IPlugin.ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"GameToRole.dll v0.3.1 Plugin Loading...");
            Games.GameManager.Instance.StartGameManager(DiscordClient);
        }

        async Task IPlugin.CommandAsync(string command, string message, SocketMessage sktMessage)
        {
            var splitCommandString = SplitArguments(message);
            var commandHandler = new Admin.Handler
                {DiscordSocket = sktMessage};

            commandHandler.Parse(splitCommandString, DiscordClient);
        }

        public static string[] SplitArguments(string commandLine)
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

        Task IPlugin.Message(string message, SocketMessage sktMessage)
        {
            return Task.CompletedTask;
        }

        void IPlugin.Dispose()
        {
            GlobalLogger.Logger.Instance.WriteConsole("GameToRole Disposed");
        }
    }
}