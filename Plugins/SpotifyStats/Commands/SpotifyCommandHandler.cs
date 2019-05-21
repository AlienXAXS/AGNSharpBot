using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using SpotifyStats.SQLite.Tables;

namespace SpotifyStats.Commands
{
    class SpotifyCommandHandler
    {
        private struct TopEntry
        {
            public string Username;
            public int PlayCount;
        }

        [Command("spotify","Gets information about Spotify usage on this Discord server (try !spotify help)")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void Spotify(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`Spotify System`\r\n" +
                                                          "Try !spotify help for commands");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("\r\n`Spotify System Help`\r\n" +
                                                        "`!spotify top [number]`\r\nDisplays the top three Spotify listerers, optionally you can specify how many top listeners you want, for example !spotify top 10");
                    break;
                case "top":
                    await HandleTop(parameters, sktMessage, discordSocketClient);
                    break;
            }
        }

        [Command("spotifyadmin", "Spotify admin commands - try !spotifyadmin help")]
        public async void SpotifyAdmin(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`Spotify System`\r\n" +
                                                          "Try !spotifyadmin help for commands");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("\r\n`Spotify Admin System Help`\r\n" +
                                                              "`!spotifyadmin rm_stale_users`\r\nForcefully removes any users that exist in the Spotify database that are no longer part of this Discord server.");
                    break;

                case "rm_stale_users":
                    await RemoveStaleSpotifyUsers(parameters, sktMessage, discordSocketClient);
                    break;
            }
        }

        private async Task RemoveStaleSpotifyUsers(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var listeners = InternalDatabase.Handler.Instance.GetConnection();
            var listenersTable = listeners.DbConnection.Table<SQLite.Tables.Listener>();

            var unknownUsers = (from listener in listenersTable.GroupBy(x => x.DiscordId) where discordSocketClient.GetUser((ulong) listener.Key) == null select (ulong) listener.Key).ToList();

            foreach (var unknownUser in unknownUsers)
            {
                var uid = (long) unknownUser;
                listenersTable.Delete(x => x.DiscordId == uid);
            }

            await sktMessage.Channel.SendMessageAsync($"Removed {unknownUsers.Count} users from the Spotify Database");
        }

        private async Task HandleTop(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var topLength = 3;

            // Check for a custom top
            if (parameters.Length == 3)
            {
                if (!int.TryParse(parameters[2], out topLength))
                {
                    await sktMessage.Channel.SendMessageAsync(
                        "Unable to process your request, the optional parameter must be a number");
                }
                else
                {
                    // We don't want a user requesting 999999 top...
                    if (topLength > 20) topLength = 20;
                }
            }

            var listeners = InternalDatabase.Handler.Instance.GetConnection()?.DbConnection.Table<SQLite.Tables.Listener>();
            if ( listeners == null ) throw new Exception("DB FAIL IN SPOTIFY");

            var group = listeners.GroupBy(x => x.DiscordId);
            var topTake = group.OrderByDescending(x => x.Count()).Take(topLength);

            var topUsersList = (from topEntry in topTake let foundUser = discordSocketClient.GetUser((ulong) topEntry.Key) select new TopEntry() {Username = foundUser != null ? foundUser.Username : "Unknown User", PlayCount = topEntry.Count()}).ToList();

            var discordEmbedBuilder = new EmbedBuilder();
            discordEmbedBuilder.WithTitle($"Top {topLength} Spotify Users");

            var outputString = "";
            for (var i = 0; i <= topUsersList.Count - 1; i++)
            {
                outputString +=
                    $"{i + 1}. {topUsersList[i].Username}: Played {topUsersList[i].PlayCount} song(s)\r\n";
            }
            discordEmbedBuilder.AddField("Users", outputString);

            await sktMessage.Channel.SendMessageAsync(embed: discordEmbedBuilder.Build());
        }
    }
}
