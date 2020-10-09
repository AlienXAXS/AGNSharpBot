using System;
using System.Collections.Generic;
using System.Linq;
using CommandHandler;
using Discord.WebSocket;

namespace GameGiveaway.Commands
{
    internal class GameGiveawayAdmin
    {
        [Command("ggadmin", "Game Giveaway Admin Commands")]
        public async void GameGiveawayAdminCmd(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            var gamesAdded = new List<string>();
            var gamesErrored = new List<string>();

            var allGamesDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<Responses.Commands.GameGiveaway.SQL.GameGiveawayGameDb>();

            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("Invalid use of command");
                return;
            }

            foreach (var game in parameters.Where(x => !x.Equals("!ggadmin")))
            {
                var gameSplit = game.Split('|');
                if (gameSplit.Length != 2)
                {
                    gamesErrored.Add($"{game} [Error: Game is not formatted correctly - Game Name|Key]");
                }
                else
                {
                    // Grab the game and the key, split by a '|'
                    var gameName = gameSplit[0].Trim();
                    var gameKey = gameSplit[1].Trim();

                    if (allGamesDb.Any(x => x != null && x.Key.ToLower().Equals(gameKey.ToLower())))
                    {
                        // Game already exists, oops
                        gamesErrored.Add($"{game} [Error: Game key already exists in db]");
                        continue;
                    }

                    var newGame = new Responses.Commands.GameGiveaway.SQL.GameGiveawayGameDb()
                    { Key = gameKey, Name = gameName };

                    try
                    {
                        allGamesDb.Connection.Insert(newGame);
                        gamesAdded.Add(game);
                    }
                    catch (Exception ex)
                    {
                        gamesErrored.Add($"{game} [Error: SQLDB Error: {ex.Message}]");
                    }
                }
            }

            await sktMessage.Channel.SendMessageAsync(
                $"" + $"Games Added: {(gamesAdded.Count > 0 ? gamesAdded.Aggregate((x, y) => x + Environment.NewLine + y) : "None")}\r\n" +
                $"Games Errored: { (gamesErrored.Count > 0 ? gamesErrored.Aggregate((x, y) => x + Environment.NewLine + y) : "None")}");
        }
    }
}