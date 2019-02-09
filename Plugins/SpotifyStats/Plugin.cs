using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using PluginInterface;
using Discord.WebSocket;

namespace SpotifyStats
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Spotify Stats";
        public DiscordSocketClient DiscordClient { get; set; }
        List<string> IPlugin.Commands => null;
        List<PluginRequestTypes.PluginRequestType> IPlugin.RequestTypes => null;

        public async void ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"SpotifyStats.dll Plugin Loading...");

            // Setup our discordclient link to spotifystats
                await Spotify.SpotifyHandler.Instance.SetupDiscordInstance(DiscordClient);
        }

        public void Dispose()
        {
            
        }

        public Task Message(string message, SocketMessage sktMessage)
        {
            return Task.CompletedTask;
        }

        public Task CommandAsync(string command, string message, SocketMessage sktMessage)
        {
            return Task.CompletedTask;
        }
    }
}
