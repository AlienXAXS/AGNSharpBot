using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;

namespace Responses.Commands.GameGiveaway
{
    class GameGiveawayPublic
    {
        private const double GiveawayAccessDurationInDays = 30.0;

        [Command("gamegiveaway", "Game Giveaway - Get free Steam Keys")]
        [Alias("gg")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void GameGiveawayCmd(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            try
            {
                var gamesDbConnection = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                    .Table<SQL.GameGiveawayGameDb>();

                var gamesDb = gamesDbConnection.Where(x => x.Used == false);

                var usersDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                    .Table<SQL.GameGiveawayUserDb>();

                if (!gamesDb.Any())
                {
                    await sktMessage.Channel.SendMessageAsync(
                        "Sorry, I do not have any free games to give away at the moment, try again later though!");
                    return;
                }

                var dbUser = usersDb.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.DiscordId.Equals((long) sktMessage.Author.Id));
                if (dbUser != null)
                {
                    var lastAccessTimespan = (dbUser.DateTime.AddDays(30) - DateTime.Now);
                    if (lastAccessTimespan.TotalDays < GiveawayAccessDurationInDays)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Sorry {sktMessage.Author.Username}, but you'll have to wait {Util.ReadableTimespan.GetReadableTimespan(lastAccessTimespan)} before you can claim another game.");
                        return;
                    }
                    else
                    {
                        // Remove the user from the table, they will be re-added shortly
                        usersDb.Connection.Delete(dbUser);
                    }
                }

                // Code will only reach here if the user either isnt in the claims database, or if they didnt claim within the last GiveawayAccessDurationInDays value

                // Insert our user into the database
                usersDb.Connection.Insert(new SQL.GameGiveawayUserDb() {DiscordId = (long) sktMessage.Author.Id, DateTime = DateTime.Now});

                // Grab a random game from the database, modify it to be marked as used and send a PM to the user with the key.
                var rand = new Random(DateTime.Now.Millisecond);
                var givenGame = gamesDb.ElementAt(rand.Next(gamesDb.Count()));
                givenGame.Used = true;
                gamesDb.Connection.Update(givenGame);

                await sktMessage.Author.SendMessageAsync(
                    $"Free Games - By AlienX's Gaming Network\r\n\r\nYour Free Game:\r\nName: `{givenGame.Name}`\r\nKey: `{givenGame.Key}`\r\n\r\nPlease activate this game on steam.");

                await sktMessage.Channel.SendMessageAsync($"Free Games - By AlienX's Gaming Network\r\n" +
                                                          $"Congratulations {sktMessage.Author.Username}!  -  You have been given a free copy of `{givenGame.Name}`." +
                                                          $"\r\n" +
                                                          $"Claim your own free game - simply type !gamegiveaway (or !gg for short) in chat.");
            }
            catch (Exception ex)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"Sorry, an error stopped you from getting your free game. Please report this to AlienX.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }
    }
}