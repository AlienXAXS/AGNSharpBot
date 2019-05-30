using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using PluginInterface;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;

namespace SpotifyStats
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Spotify Stats";
        public DiscordSocketClient DiscordClient { get; set; }

        public void ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() {Compress = true});

            AdvancedLoggerHandler.Instance.GetLogger().Log($"SpotifyStats.dll Plugin Loading...");
            
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

    }
}
