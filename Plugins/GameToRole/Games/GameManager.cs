using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using GlobalLogger;
using Newtonsoft.Json;

namespace GameToRole.Games
{
    class GameManager
    {
        // Some clever code that creates a single instance of the GameManager class, and it can be used anywhere in the code from itself.
        //      Saves having to create an instance inside Program() and use that constantly.
        //      It also means you can use the same instance over any loaded plugin, anywhere.
        private static GameManager _instance;
        public static GameManager Instance = _instance ?? (_instance = new GameManager());

        // Holds the game entries - loaded from JSON at some point.
        private readonly List<GameEntry> _gameEntries = new List<GameEntry>();

        // Is just a useful var for the Discord client socket
        private DiscordSocketClient _discordSocket;

        private const string GameManagerConfigFilePath = "Plugins\\Config\\GameToRole.json";

        // Used to ensure single-thraded tasks on an async thread
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public GameManager()
        {
            // Load our config
            if (System.IO.File.Exists(GameManagerConfigFilePath))
            {
                Logger.Instance.WriteConsole("Game2Role -> Loading Configuration File");
                var config =
                    JsonConvert.DeserializeObject<List<GameEntry>>(
                        System.IO.File.ReadAllText(GameManagerConfigFilePath));

                // Load out game entries from the config to our class
                foreach (var x in config)
                    _gameEntries.Add(x);

                Logger.Instance.WriteConsole($"Game2Role -> Loaded {_gameEntries.Count} game entries");
            }
            else
                Logger.Instance.WriteConsole("Game2Role -> Unable to find configuration file, starting as new.");
        }

        public List<GameEntry> GetGameEntries()
        {
            return _gameEntries;
        }

        public Task StartGameManager(DiscordSocketClient discordSocket)
        {
            _discordSocket = discordSocket;
            _discordSocket.GuildMemberUpdated += DiscordSocketOnGuildMemberUpdatedAsync;

            Logger.Instance.WriteConsole("StartGameManager Loaded - Now listening for user update events");

            return Task.CompletedTask;
        }

        public async Task ScanAllUsers(SocketMessage sktMessage)
        {
            foreach (var x in _discordSocket.Guilds.First(x => x.IsConnected).Users)
            {
                if ( x.Activity == null ) continue;

                await sktMessage.Channel.SendMessageAsync($"Scanning User {x.Username}");
                await DiscordSocketOnGuildMemberUpdatedAsync(x, x);
            }
        }

        public async Task DeleteGameEntry(GameEntry gameEntry, SocketGuild guild)
        {
            try
            {
                await Logger.Instance.Log(
                    $"Attempting to delete game entry {gameEntry.Name} | {gameEntry.DiscordRoleId} from Guild {guild.Id}",
                    Logger.LoggerType.ConsoleOnly);

                var _role = guild.GetRole(gameEntry.DiscordRoleId);
                if (_role == null) return;

                await _role.DeleteAsync();
                _gameEntries.Remove(gameEntry);
                await SaveGameEntries();

                await Logger.Instance.Log(
                    $"Game Entry {gameEntry.Name} | {gameEntry.DiscordRoleId} was deleted from Guild {guild.Id} - {guild.Name}",
                    Logger.LoggerType.ConsoleAndDiscord);
            }
            catch (Exception ex)
            {
                await Logger.Instance.Log(
                    $"Unhandled Exception while running task\r\n{ex.Message}\r\n\r\n\r\n{ex.StackTrace}", Logger.LoggerType.ConsoleOnly);
            }
        }

        private async Task DiscordSocketOnGuildMemberUpdatedAsync(SocketGuildUser guildUserBefore, SocketGuildUser guildUserAfter)
        {
            // As we're doing file operations, only allow this method to run single threaded, even if multiple threads are waiting upon it
            await SemaphoreSlim.WaitAsync();

            try
            {
                RestRole createdRestRole = null;

                if (guildUserAfter.Activity != null && guildUserAfter.Activity.Type == ActivityType.Playing)
                {
                    var playingActivity = (Game) guildUserAfter.Activity;
                    if (playingActivity.Name.Equals("")) return;

                    // First check if the game already exists, if so add the user to the role
                    var gameFinder = _gameEntries.Where(x =>
                            string.Equals(x.Name, playingActivity.Name, StringComparison.CurrentCultureIgnoreCase))
                        .DefaultIfEmpty(null).FirstOrDefault();

                    // First check if the user is already part of that role, if they are there is no need to keep going.
                    if (guildUserAfter.Roles.Where(x => gameFinder != null && x.Id == gameFinder.DiscordRoleId)
                            .DefaultIfEmpty(null)
                            .FirstOrDefault() != null)
                        return;

                    // if it's the first one, check the roles, otherwise check the game entries
                    if (_gameEntries == null)
                    {
                        // If an active role in the guild is the same name as an already existing role, but it's not in the game database, then deny access
                        var gameRoleDetector = guildUserAfter.Guild.Roles
                            .Where(x => x.Name.Equals(playingActivity.Name,
                                StringComparison.CurrentCultureIgnoreCase))
                            .DefaultIfEmpty(null).FirstOrDefault();

                        if (gameRoleDetector != null)
                        {
                            await Logger.Instance.Log(
                                $"WARNING: {guildUserAfter.Username} attempted to auto-join a role called {playingActivity.Name} which is not meant for them", Logger.LoggerType.ConsoleAndDiscord);

                            return;
                        }
                    }

                    if (gameFinder == null)
                    {
                        // The game is new, set it up
                        // Create the role
                        try
                        {
                            await Logger.Instance.Log(
                                $"GAME2ROLE - Found a new game, Creating role for it [{playingActivity.Name}]", Logger.LoggerType.ConsoleOnly);

#if DEBUG
                            await Logger.Instance.Log("We're in debug, not going to make new roles", Logger.LoggerType.ConsoleOnly);
                            return;
#endif

                            var permissions = new GuildPermissions();
                            createdRestRole =
                                await guildUserAfter.Guild.CreateRoleAsync(playingActivity.Name, permissions);

                            await createdRestRole.ModifyAsync(properties => properties.Mentionable = true);

                            await Logger.Instance.Log(
                                $"GAME2ROLE -    -> Role Created [{createdRestRole.Id}]", Logger.LoggerType.ConsoleOnly);

                            _gameEntries.Add(new GameEntry(playingActivity.Name, createdRestRole.Id));

                            await guildUserAfter.AddRoleAsync(createdRestRole);

                            await SaveGameEntries();
                        }
                        catch (Exception ex)
                        {
                            // Something went wrong
                            Logger.Instance.WriteConsole(
                                $"ERROR\r\n{ex.Message}\r\n\r\nSTACK:\r\n{ex.StackTrace}");

                            // Cleanup the role
                            if (createdRestRole != null)
                                await createdRestRole.DeleteAsync();
                        }
                    }
                    else
                    {
                        var roleAlreadyExistsByNameResult = guildUserAfter.Guild.Roles
                            .Where(x => x.Name == playingActivity.Name)
                            .DefaultIfEmpty(null).FirstOrDefault();

                        // The game is already known, get the role ID and add to it
                        Logger.Instance.WriteConsole(
                            $"{guildUserAfter.Username} is playing {playingActivity.Name} - Game exists in DB");

                        var gameRole = guildUserAfter.Guild.Roles.Where(x => x.Id == gameFinder.DiscordRoleId)
                            .DefaultIfEmpty(null).FirstOrDefault();

                        if (gameRole == null)
                        {
                            // We cannot find a game role with the ID, for some reason - so if we have an actual role with the correct ID we'll use that.
                            if (roleAlreadyExistsByNameResult != null)
                            {
                                // Add a new entry and re-save to disk
                                _gameEntries.Add(new GameEntry(playingActivity.Name,
                                    roleAlreadyExistsByNameResult.Id));
                                await SaveGameEntries();

                                Logger.Instance.WriteConsole(
                                    $"  -> Adding user to role with ID {roleAlreadyExistsByNameResult.Id}");
                                await guildUserBefore.AddRoleAsync(roleAlreadyExistsByNameResult);
                            }
                        }
                        else
                        {
                            // Now just as a sanity check, check to see if a role with the name already exists and that the ID matches that we have in the DB.
                            if (roleAlreadyExistsByNameResult != null)
                            {
                                if (roleAlreadyExistsByNameResult.Id != gameRole.Id)
                                {
                                    // Update our DB on this change
                                    gameFinder.DiscordRoleId = roleAlreadyExistsByNameResult.Id;
                                    await SaveGameEntries();
                                }
                            }

                            Logger.Instance.WriteConsole(
                                $"  -> Adding user to role with ID {gameRole.Id}");
                            await guildUserBefore.AddRoleAsync(gameRole);
                        }
                    }

                    await Logger.Instance.Log(
                        $"User {guildUserBefore.Username} is playing {playingActivity.Name}, Roles adjusted", Logger.LoggerType.ConsoleAndDiscord);

                }
            }
            catch
            {
                // ignored
                //todo: dont ignore this
            }
            finally
            {
                // Release the current Semaphore, allowing another queued thread access to this one
                SemaphoreSlim.Release();
            }
        }

        private async Task SaveGameEntries()
        {
#if DEBUG

            await Logger.Instance.Log("We're in debug, not going to save new roles", Logger.LoggerType.ConsoleOnly);
            return;
#endif

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(GameManagerConfigFilePath)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GameManagerConfigFilePath));

            System.IO.File.WriteAllText(GameManagerConfigFilePath, JsonConvert.SerializeObject(_gameEntries, Formatting.Indented));
          
        }
    }
}
