using GlobalLogger.AdvancedLogger;
using Interface;
using PluginManager;
using System.ComponentModel.Composition;

namespace SpotifyStats
{
    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "Spotify Stats";

        string IPlugin.Description => "Spotify based stats, such as top listeners.";

        public void ExecutePlugin()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() { Compress = true });

            AdvancedLoggerHandler.Instance.GetLogger().Log($"SpotifyStats.dll Plugin Loading...");

            // Register our tables with the SQLHandler
            var dbConn = InternalDatabase.Handler.Instance.NewConnection();
            dbConn.RegisterTable<SQLite.Tables.Listener>();
            dbConn.RegisterTable<SQLite.Tables.Song>();

            // Setup our discordclient link to spotifystats
            Spotify.SpotifyHandler.Instance.SetupDiscordInstance(EventRouter);
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.SpotifyCommandHandler>();
        }

        public void Dispose()
        {
        }

        public EventRouter EventRouter { get; set; }
    }
}