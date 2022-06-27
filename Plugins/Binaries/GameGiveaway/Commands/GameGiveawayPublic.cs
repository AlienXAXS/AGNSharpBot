using System;
using System.Linq;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using DiscordMenu;
using GameGiveaway.Util;
using InternalDatabase;
using Responses.Commands.GameGiveaway.SQL;

namespace GameGiveaway.Commands
{
    internal class GameGiveawayPublic
    {
        private readonly int GiveawayAccessDurationInDays = 30;

        [Command("gamegiveaway", "Game Giveaway - Get free Steam Keys")]
        [Alias("gg", "ggz")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void GameGiveawayCmd(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var author = sktMessage.Author;
            if (author is SocketGuildUser guildUser)
            {
                var joinedRecently = DateTime.Now - guildUser.JoinedAt;
                if (joinedRecently.Value.TotalDays < 30)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        $"{author.Username}.  Thanks for trying to use the Game Giveaway system, but you are too new to use it.  You can try again later!");
                    return;
                }
            }

            if (sktMessage.Author.Id.Equals(419551871443927082))
            {
                await sktMessage.Channel.SendMessageAsync(
                    "Claim your own free game - simply type !gamegiveaway (or !gg for short) in chat.");
                await sktMessage.DeleteAsync();
            }
            else
            {
                DoGiveaway(sktMessage);
            }
        }

        private async void DoGiveaway(SocketMessage SktMessage)
        {
            try
            {
                var gamesDbConnection = Handler.Instance.GetConnection().DbConnection.Table<GameGiveawayGameDb>();
                var gamesDb = gamesDbConnection.Where(x => x.Used == false);
                var usersDb = Handler.Instance.GetConnection().DbConnection.Table<GameGiveawayUserDb>();
                var dbUser = usersDb.DefaultIfEmpty(null)
                    .LastOrDefault(x => x != null && x.DiscordId.Equals((long)SktMessage.Author.Id));

                if (dbUser != null)
                {
                    dbUser.isHumbleRegistered = false;
                    usersDb.Connection.Update(dbUser);
                }
                else
                {
                    usersDb.Connection.Insert(new GameGiveawayUserDb
                    {
                        DateTime = DateTime.Now,
                        DiscordId = (long)SktMessage.Author.Id,
                        isHumbleRegistered = false
                    });
                }

                if (!gamesDb.Any())
                {
                    await SktMessage.Channel.SendMessageAsync(
                        "Sorry, I do not have any free games to give away at the moment.  You can try again later though!");
                    return;
                }

                // Time check
                if (dbUser != null)
                {
                    var lastAccessTimespan = dbUser.DateTime.AddDays(GiveawayAccessDurationInDays) - DateTime.Now;
                    if (lastAccessTimespan.TotalDays > 0)
                    {
                        await SktMessage.Channel.SendMessageAsync(
                            $"Sorry {SktMessage.Author.Username}, but you'll have to wait {ReadableTimespan.GetReadableTimespan(lastAccessTimespan)} before you can claim another game.");
                        return;
                    }
                }

                // Insert our user into the database
                if (dbUser == null)
                {
                    usersDb.Connection.Insert(new GameGiveawayUserDb
                    { DiscordId = (long)SktMessage.Author.Id, DateTime = DateTime.Now });
                }
                else
                {
                    dbUser.DateTime = DateTime.Now;
                    usersDb.Connection.Update(dbUser);
                }

                // Grab a random game from the database, modify it to be marked as used and send a PM to the user with the key.
                var rand = new Random(DateTime.Now.Millisecond);
                var givenGame = gamesDb.ElementAt(rand.Next(gamesDb.Count()));
                givenGame.Used = true;
                gamesDb.Connection.Update(givenGame);

                await SktMessage.Author.SendMessageAsync(
                    $"Free Games - By AlienX's Gaming Network\r\n\r\nYour Free Game:\r\nName: `{givenGame.Name}`\r\nKey: `{givenGame.Key}`\r\n\r\nPlease activate this game on steam (unless specified otherwise).");

                await SktMessage.Channel.SendMessageAsync("Free Games - By AlienX's Gaming Network\r\n" +
                                                          $"Congratulations {SktMessage.Author.Username}!  -  You have been given a free copy of `{givenGame.Name}`." +
                                                          "\r\n" +
                                                          "Claim your own free game - simply type !gamegiveaway (or !gg for short) in chat.");
            }
            catch (Exception ex)
            {
                await SktMessage.Channel.SendMessageAsync(
                    $"Sorry, an error stopped you from getting your free game. Please report this to AlienX.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }
    }
}