using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger;

namespace SpotifyStats.Spotify
{
    class SpotifyHandler
    {
        // Instancing handler for this class
        private static SpotifyHandler _instance;
        public static SpotifyHandler Instance = _instance ?? (_instance = new SpotifyHandler());

        private InternalDatabase.Connection dbConnection;

        public DiscordSocketClient _discordSocketClient;
        public void SetupDiscordInstance(DiscordSocketClient discordSocket)
        {
            _discordSocketClient = discordSocket;
            _discordSocketClient.GuildMemberUpdated += OnGuildMemberUpdated;
            _discordSocketClient.UserLeft += DiscordSocketClientOnUserLeft;

            dbConnection = InternalDatabase.Handler.Instance.GetConnection();
        }

        private Task DiscordSocketClientOnUserLeft(SocketGuildUser socketGuildUser)
        {
            try
            {
                long lId = (long) socketGuildUser.Id;
                dbConnection.DbConnection.Table<SQLite.Tables.Listener>()
                    .Delete(x => x.DiscordId == lId);
            }
            catch (Exception)
            {

            }

            return Task.CompletedTask;
        }

        private struct TopPlayEntry
        {
            public string Username;
            public int PlayCount;
        }

        private async Task OnGuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            try
            {
                //Ensure that the previous member and the new one do not contain the same listening data.
                if (oldMember?.Activity is SpotifyGame sGameOld)
                {
                    if (newMember.Activity is SpotifyGame sGameNew)
                    {
                        if (sGameOld.TrackId == sGameNew.TrackId)
                            return;
                    }
                }

                // check to see if the new member has an activity
                if (newMember.Activity?.Type == ActivityType.Listening)
                {
                    // It's spotify listing type
                    await Logger.Instance.Log($"User {newMember.Username} is listening to spotify",
                        Logger.LoggerType.ConsoleOnly);

                    if (newMember.Activity is SpotifyGame spotifyGame)
                    {
                        var returnedSong = SQLite.SQLiteHandler.AddSongAndListener(dbConnection, spotifyGame.TrackId, spotifyGame.Artists.First(), spotifyGame.TrackTitle, newMember.Id);
                        var group = returnedSong.Listeners.GroupBy(x => x.DiscordId).OrderByDescending(x => x.Count());

                        var top = group.Take(3);

                        var topUsersList = (from topEntry in top let foundUser = newMember.Guild.GetUser((ulong) topEntry.Key) where foundUser != null select new TopPlayEntry() {Username = foundUser.Username, PlayCount = topEntry.Count()}).ToList();

                        var discordEmbedBuilder = new EmbedBuilder();
                        discordEmbedBuilder.WithTitle($"{returnedSong.Song.Artist} - {returnedSong.Song.Name}")
                            .WithThumbnailUrl(spotifyGame.AlbumArtUrl).WithUrl(spotifyGame.TrackUrl);

                        discordEmbedBuilder.AddField("Play Count", returnedSong.Listeners.Count, true);
                        discordEmbedBuilder.AddField("Person Listening",
                            newMember?.Nickname ?? newMember.Username, true);

                        await Logger.Instance.Log(
                            $"User {newMember.Username} Listening to Spotify | SID:{spotifyGame.SessionId} | ID: {spotifyGame.TrackId} | A:{spotifyGame.Artists.First()} | T: {spotifyGame.TrackTitle}",
                            Logger.LoggerType.ConsoleOnly);

                        var outputString = "";
                        for (var i = 0; i <= topUsersList.Count - 1; i++)
                        {
                            outputString +=
                                $"{i + 1}. {topUsersList[i].Username}: Played {topUsersList[i].PlayCount} time(s)\r\n";
                        }

                        discordEmbedBuilder.AddField("Top Users", outputString);
                        
                        await Logger.Instance.Log(null, Logger.LoggerType.ConsoleAndDiscord,
                            discordEmbed: discordEmbedBuilder.Build());
                    }
                }
            }
            catch(Exception ex)
            {
                await Logger.Instance.Log($"FATAL EXCEPTION\r\n{ex.Message}\r\n\r\nSTACK:\r\n{ex.StackTrace}))",
                    Logger.LoggerType.ConsoleAndDiscord);
            }
        }
    }
}
