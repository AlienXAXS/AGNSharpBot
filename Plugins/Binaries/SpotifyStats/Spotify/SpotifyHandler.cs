﻿using System;
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
                //Ensure that the previous member and the new one do not contain the same listening data.
                if (oldMember?.Activity is SpotifyGame sGameOld)
                    if (newMember.Activity is SpotifyGame sGameNew)
                        if (sGameOld.TrackId == sGameNew.TrackId)
                            return;

                // check to see if the new member has an activity
                if (newMember.Activity?.Type == ActivityType.Listening)
                {
                    // It's spotify listing type
                    Log4NetHandler.Log($"User {newMember.Username} is listening to spotify",
                        Log4NetHandler.LogLevel.DEBUG);

                    if (newMember.Activity is SpotifyGame spotifyGame)
                    {
                        var returnedSong = SQLiteHandler.AddSongAndListener(dbConnection, spotifyGame.TrackId,
                            spotifyGame.Artists.First(), spotifyGame.TrackTitle, newMember.Id);
                        var group = returnedSong.Listeners.GroupBy(x => x.DiscordId).OrderByDescending(x => x.Count());

                        var top = group.Take(3);

                        var topUsersList = (from topEntry in top
                                let foundUser = newMember.Guild.GetUser((ulong) topEntry.Key)
                                where foundUser != null
                                select new TopPlayEntry {Username = foundUser.Username, PlayCount = topEntry.Count()})
                            .ToList();

                        var discordEmbedBuilder = new EmbedBuilder();
                        discordEmbedBuilder.WithTitle($"{returnedSong.Song.Artist} - {returnedSong.Song.Name}")
                            .WithThumbnailUrl(spotifyGame.AlbumArtUrl).WithUrl(spotifyGame.TrackUrl);

                        discordEmbedBuilder.AddField("Play Count", returnedSong.Listeners.Count, true);
                        discordEmbedBuilder.AddField("Person Listening",
                            newMember?.Nickname ?? newMember.Username, true);

                        Log4NetHandler.Log(
                            $"User {newMember.Username} Listening to Spotify | SID:{spotifyGame.SessionId} | ID: {spotifyGame.TrackId} | A:{spotifyGame.Artists.First()} | T: {spotifyGame.TrackTitle}",
                            Log4NetHandler.LogLevel.DEBUG);

                        var outputString = "";
                        for (var i = 0; i <= topUsersList.Count - 1; i++)
                            outputString +=
                                $"{i + 1}. {topUsersList[i].Username}: Played {topUsersList[i].PlayCount} time(s)\r\n";

                        discordEmbedBuilder.AddField("Top Users", outputString);

                        //TODO: Fix this shit
                        //GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log(null, GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.LoggerType.ConsoleAndDiscord, discordEmbed: discordEmbedBuilder.Build());
                    }
                }
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