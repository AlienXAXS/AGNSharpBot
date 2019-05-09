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
            
            // Register our tables with the SQLHandler
            var dbConn = InternalDatabase.Handler.Instance.NewConnection();
            dbConn.RegisterTable<SQLite.Tables.Listener>();
            dbConn.RegisterTable<SQLite.Tables.Song>();

            // Setup our discordclient link to spotifystats
            Spotify.SpotifyHandler.Instance.SetupDiscordInstance(DiscordClient);
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.SpotifyCommandHandler>();
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
