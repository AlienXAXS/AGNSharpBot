using CommandHandler;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using Discord;

namespace GameWatcher.Commands
{
    internal class Control
    {
        [Command("gamewatcher", "Manages the Game Watcher, add and remove games to watch for.")]
        public async void GameWatcher(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (InternalDatabase.Handler.Instance.GetConnection() == null)
            {
                await sktMessage.Channel.SendMessageAsync($"`Game Watcher Control`\r\nUnable to connect to the internal database.");
                return;
            }

            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync($"`Game Watcher Control`\r\nTry !gamewatcher help.");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync($"`Game Watcher Control`\r\n" +
                                                              "`!gamewatcher add \"GAME NAME\"` - Adds a game to the database.\r\n" +
                                                              "`!gamewatcher remove \"GAME NAME\"` - Removes a game from the database.\r\n" +
                                                              "`!gamewatcher list` - Lists all games that are added to the database.\r\n" +
                                                              "`!gamewatcher scan` - Scans all users for games.");
                    break;

                case "add":
                    if (parameters.Length != 3)
                    {
                        await sktMessage.Channel.SendMessageAsync($"Invalid command syntax");
                        return;
                    }
                    AddGame(parameters[2], sktMessage);
                    break;

                case "remove":
                    if (parameters.Length != 3)
                    {
                        await sktMessage.Channel.SendMessageAsync($"Invalid command syntax");
                        return;
                    }
                    RemoveGame(parameters[2], sktMessage);
                    break;

                case "list":
                    ListGames(sktMessage);
                    break;

                case "scan":
                    ScanUsers(discordSocketClient, sktMessage);
                    break;

                case "rolepos":
                    var msg = "";
                    foreach (var x in discordSocketClient.Guilds)
                        foreach (var y in x.Roles)
                        {
                            if (!y.IsEveryone)
                                msg += $"{y.Name} = {y.Position}\r\n";
                        }

                    await sktMessage.Channel.SendMessageAsync(msg);
                    break;
            }
        }

        private async void ScanUsers(DiscordSocketClient discordSocketClient, SocketMessage sktMessage)
        {
            await sktMessage.Channel.SendMessageAsync(
                "Scanning this guild for users playing games that exist in the database, this takes 1 second per user active in your guild.");

            if (sktMessage.Author is SocketGuildUser socketGuildUser)
            {
                var guild = socketGuildUser.Guild;
                if (!PluginManager.PluginHandler.Instance.PluginRouter.IsPluginExecutableOnGuild(guild.Id))
                    return;

                var uList = guild.Users.ToList();

                foreach (var user in uList)
                {
                    try
                    {
                        if (user.Activity != null && user.Activity is Game)
                        {
                            await GameHandler.Instance.GameScan(user, user);
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }
                }

                await sktMessage.Channel.SendMessageAsync("Scan completed");
            }
        }

        private async void ListGames(SocketMessage sktMessage)
        {
            var message = "";
            var _db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<DB.Tables.GameMemory>();
            foreach (var game in _db)
            {
                message += $"{game.Name}\r\n";
            }

            await sktMessage.Channel.SendMessageAsync($"Games registered in the database:\r\n{message}");
        }

        private async void AddGame(string gameName, SocketMessage sktMessage)
        {
            try
            {
                if (DB.DatabaseHandler.Instance.Exists(gameName))
                    await sktMessage.Channel.SendMessageAsync("Unable to add this game, it already exists");
                else
                {
                    DB.DatabaseHandler.Instance.Add(gameName);
                    await sktMessage.Channel.SendMessageAsync($"Game {gameName} has been added to the GameWatcher");
                }
            }
            catch (Exception ex)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"Database failure.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }

        private async void RemoveGame(string gameName, SocketMessage sktMessage)
        {
            try
            {
                if (!DB.DatabaseHandler.Instance.Exists(gameName))
                    await sktMessage.Channel.SendMessageAsync(
                        $"Unable to remove the game {gameName} as it does not exist in the database");
                else
                {
                    DB.DatabaseHandler.Instance.Remove(gameName);
                    await sktMessage.Channel.SendMessageAsync($"Game {gameName} has been removed");
                }
            }
            catch (Exception ex)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"Database failure.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }
    }
}