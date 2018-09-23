using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        private DiscordSocketClient _discordSocketClient;
        public async Task SetupDiscordInstance(DiscordSocketClient discordSocket)
        {
            _discordSocketClient = discordSocket;
            _discordSocketClient.GuildMemberUpdated += OnGuildMemberUpdated;
            await Database.SpotifySongDatabase.Instance.LoadTracks();
        }

        private static async Task OnGuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
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
                        var artistName = spotifyGame.Artists.First();
                        var dbEntry =
                            Database.SpotifySongDatabase.Instance.AddTrack(artistName, spotifyGame.TrackTitle);

                        var discordEmbedBuilder = new EmbedBuilder();
                        discordEmbedBuilder.WithTitle($"{artistName} - {spotifyGame.TrackTitle}")
                            .WithThumbnailUrl(spotifyGame.AlbumArtUrl).WithUrl(spotifyGame.TrackUrl);

                        discordEmbedBuilder.AddField("Play Count", dbEntry.PlayCount, true);
                        discordEmbedBuilder.AddField("Person Listening",
                            newMember?.Nickname ?? newMember.Username, true);

                        await Logger.Instance.Log(null, Logger.LoggerType.ConsoleAndDiscord,
                            discordEmbed: discordEmbedBuilder.Build());
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
