using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger;
using InternalDatabase;
using PluginManager;
using SpotifyStats.SQLite;
using SpotifyStats.SQLite.Tables;

namespace SpotifyStats.Spotify
{
    internal class SpotifyHandler
    {
        // Instancing handler for this class
        private static readonly SpotifyHandler _instance;

        public static SpotifyHandler Instance = _instance ?? (_instance = new SpotifyHandler());

        private Connection dbConnection;

        public void SetupDiscordInstance(EventRouter discordSocket)
        {
            discordSocket.GuildMemberUpdated += OnGuildMemberUpdated;
            discordSocket.UserLeft += DiscordSocketClientOnUserLeft;

            dbConnection = Handler.Instance.GetConnection();
        }

        private Task DiscordSocketClientOnUserLeft(SocketGuildUser socketGuildUser)
        {
            try
            {
                var lId = (long) socketGuildUser.Id;
                dbConnection.DbConnection.Table<Listener>().Delete(x => x.DiscordId == lId);
            }
            catch (Exception)
            {
            }

            return Task.CompletedTask;
        }

        private async Task OnGuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            try
            {
                SpotifyGame newSpotifyInfo = (SpotifyGame)newMember.Activities.DefaultIfEmpty(null)
                    .FirstOrDefault(x => x != null && x.Type == ActivityType.Listening);
                SpotifyGame oldSpotifyInfo = (SpotifyGame)oldMember.Activities.DefaultIfEmpty(null)
                    .FirstOrDefault(x => x != null && x.Type == ActivityType.Listening);

                if (newSpotifyInfo == null) return;

                //Ensure that the previous member and the new one do not contain the same listening data.
                if (oldSpotifyInfo != null)
                    if (oldSpotifyInfo.TrackId == newSpotifyInfo.TrackId)
                        return;

                // It's spotify listing type
                Log4NetHandler.Log($"User {newMember.Username} is listening to spotify", Log4NetHandler.LogLevel.DEBUG);


                SQLiteHandler.AddSongAndListener(dbConnection, newSpotifyInfo.TrackId, newSpotifyInfo.Artists.First(), newSpotifyInfo.TrackTitle, newMember.Id);
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("Unhandled Exception in SpotifyStats OnGuildMemberUpdated",
                    Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }

        private struct TopPlayEntry
        {
            public string Username;
            public int PlayCount;
        }
    }
}