using System.ComponentModel.Composition;
using CommandHandler;
using Interface;
using InternalDatabase;
using PluginManager;
using SpotifyStats.Commands;
using SpotifyStats.Spotify;
using SpotifyStats.SQLite.Tables;

namespace SpotifyStats
{
    [Export(typeof(IPlugin))]
    public sealed class PluginSpotifyStats : IPlugin
    {
        string IPlugin.Name => "Spotify Stats";

        string IPlugin.Description => "Spotify based stats, such as top listeners.";

        public void ExecutePlugin()
        {
            // Register our tables with the SQLHandler
            var dbConn = Handler.Instance.NewConnection();
            dbConn.RegisterTable<Listener>();
            dbConn.RegisterTable<Song>();

            // Setup our discordclient link to spotifystats
            SpotifyHandler.Instance.SetupDiscordInstance(EventRouter);
            HandlerManager.Instance.RegisterHandler<SpotifyCommandHandler>();
        }

        public void Dispose()
        {
        }

        public EventRouter EventRouter { get; set; }
    }
}