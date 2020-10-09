using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GameWatcher.DB.Tables;
using GlobalLogger;
using InternalDatabase;
using PluginManager;

namespace GameWatcher.Commands
{
    internal class Control
    {
        [Command("gamewatcher", "Manages the Game Watcher, add and remove games to watch for.")]
        public async void GameWatcher(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (Handler.Instance.GetConnection() == null)
            {
                await sktMessage.Channel.SendMessageAsync(
                    "`Game Watcher Control`\r\nUnable to connect to the internal database.");
                return;
            }

            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`Game Watcher Control`\r\nTry !gamewatcher help.");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("`Game Watcher Control`\r\n" +
                                                              "`!gamewatcher add \"GAME NAME\"` - Adds a game to the database.\r\n" +
                                                              "`!gamewatcher remove \"GAME NAME\"` - Removes a game from the database.\r\n" +
                                                              "`!gamewatcher list` - Lists all games that are added to the database.\r\n" +
                                                              "`!gamewatcher scan` - Scans all users for games.");
                    break;

                case "add":
                    if (parameters.Length != 3)
                    {
                        await sktMessage.Channel.SendMessageAsync("Invalid command syntax");
                        return;
                    }

                    AddGame(parameters[2], sktMessage);
                    break;

                case "remove":
                    if (parameters.Length != 3)
                    {
                        await sktMessage.Channel.SendMessageAsync("Invalid command syntax");
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
                        if (!y.IsEveryone)
                            msg += $"{y.Name} = {y.Position}\r\n";

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
                if (!PluginHandler.Instance.PluginRouter.IsPluginExecutableOnGuild(guild.Id))
                    return;

                var uList = guild.Users.ToList();

                foreach (var user in uList)
                    try
                    {
                        if (user.Activity != null && user.Activity is Game)
                        {
                            await GameHandler.Instance.GameScan(null, user);
                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }

                await sktMessage.Channel.SendMessageAsync("Scan completed");
            }
        }

        private async void ListGames(SocketMessage sktMessage)
        {
            try
            {
                if (sktMessage.Channel is SocketGuildChannel sktMessageChannel)
                {
                    var guildId = sktMessageChannel.Guild.Id;
                    var message = "";
                    var db = Handler.Instance.GetConnection().DbConnection.Table<GameMemory>();
                    var results = db.DefaultIfEmpty(null)
                        .Where(x => x != null && x.GuildId.Equals((long) sktMessageChannel.Guild.Id));

                    if (!results.Any())
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            "You have no games registered, use !gamewatcher add \"GAMENAME\" to add a new game to the database");
                    }
                    else
                    {
                        foreach (var game in db) message += $"{game.Name}\r\n";

                        await sktMessage.Channel.SendMessageAsync($"Games registered in the database:\r\n{message}");
                    }
                }
            }
            catch (Exception ex)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"There was an exception attempting to execute your command, please contact the bot author.\r\n\r\nException Details: {ex.Message}");
                Log4NetHandler.Log("Exception in GameWatcher ListGames", Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }

        private async void AddGame(string gameName, SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel sktMessageChannel)
            {
                var guildId = sktMessageChannel.Guild.Id;

                try
                {
                    if (DatabaseHandler.Instance.Exists(gameName, guildId))
                    {
                        await sktMessage.Channel.SendMessageAsync("Unable to add this game, it already exists");
                    }
                    else
                    {
                        DatabaseHandler.Instance.Add(gameName, guildId);
                        await sktMessage.Channel.SendMessageAsync($"Game {gameName} has been added to the GameWatcher");
                    }
                }
                catch (Exception ex)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        $"Database failure.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                }
            }
        }

        private async void RemoveGame(string gameName, SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel sktMessageChannel)
            {
                var guildId = sktMessageChannel.Guild.Id;
                try
                {
                    if (!DatabaseHandler.Instance.Exists(gameName, guildId))
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Unable to remove the game {gameName} as it does not exist in the database");
                    }
                    else
                    {
                        DatabaseHandler.Instance.Remove(gameName, guildId);
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
}