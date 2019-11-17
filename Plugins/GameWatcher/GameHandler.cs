using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger.AdvancedLogger;
using PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameWatcher
{
    internal class GameHandler : IDisposable
    {
        private class GameRoleMemory
        {
            public ulong RoleId { get; set; }
            public ulong UserId { get; set; }

            public GameRoleMemory(ulong roleId, ulong userId)
            {
                RoleId = roleId;
                UserId = userId;
            }
        }

        private static GameHandler _instance;
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly List<GameRoleMemory> _roleMemory = new List<GameRoleMemory>();
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public PluginRouter PluginRouter { get; set; }

        private bool _gameWatcherTimerRunning;

        private Thread gameWatcherThread;

        public GameHandler()
        {
            StartGameWatcherTimer();
        }

        public void StartGameWatcherTimer()
        {
            _gameWatcherTimerRunning = true;
            gameWatcherThread = new Thread(async () =>
            {
                while (_gameWatcherTimerRunning)
                {
                    try
                    {
                        Thread.Sleep(10000);

                        foreach (var guild in DiscordSocketClient.Guilds)
                        {
                            if (PluginRouter.IsPluginExecutableOnGuild(guild.Id))
                            {
                                foreach (var role in guild.Roles)
                                {
                                    // Check for our roles
                                    if (role.Name.StartsWith("Playing:"))
                                    {
                                        // Check for any memory of this role
                                        if (_roleMemory.All(x => x.RoleId != role.Id))
                                        {
                                            // We have none, the role should be empty, let's delete it
                                            await role.DeleteAsync();
                                        }
                                    }
                                }

                                // Remove roles from users that are in a "Playing:" role but do not have the "Playing" activity.
                                foreach (var user in guild.Users)
                                {
                                    var playingRoles = user.Roles.Where(x => x.Name.StartsWith("Playing:"));
                                    foreach (var playingRole in playingRoles)
                                    {
                                        // User is not playing anything, but they are part of a role that says they are, remove them.
                                        if (!(user.Activity is CustomStatusGame))
                                        {
                                            await user.RemoveRoleAsync(playingRole);
                                        }
                                    }

                                    if (user.Status != UserStatus.Offline)
                                        await GameScan(user, user, true);
                                }
                            }
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                }
            })
            { IsBackground = true };
            gameWatcherThread.Start();
        }

        public void Dispose()
        {
            _gameWatcherTimerRunning = false;
            gameWatcherThread?.Interrupt();
        }

        public async Task GameScan(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser, bool skipWait = false)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var randomNumber = random.Next(1000, 10000);
            var logger = AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);

            try
            {
                if (!skipWait)
                    await _semaphoreSlim.WaitAsync();

                // Grab the guild
                if (newGuildUser.Guild is SocketGuild socketGuild)
                {
                    //if (newGuildUser.Activity?.Type != ActivityType.Playing || newGuildUser.Activity?.Name != oldGuildUser.Activity?.Name)
                    if (!(newGuildUser.Activity is CustomStatusGame) || newGuildUser.Activity?.Name != oldGuildUser.Activity?.Name)
                    {
                        var foundMemory = _roleMemory?.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.UserId == newGuildUser.Id && newGuildUser.Roles.Any(y => y.Id == x.RoleId));
                        if (foundMemory == null)
                            return;

                        try
                        {
                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} had an activity of {oldGuildUser.Activity.Name} but now doesn't - attempting to find role");

                            var foundRole = socketGuild.GetRole(foundMemory.RoleId);

                            if (foundRole != null)
                            {
                                logger.Log($"[{newGuildUser.Id} | {randomNumber}] Role found, attempting to remove user from role");
                                try
                                {
                                    await newGuildUser.RemoveRoleAsync(foundRole);
                                    //logger.Log($"[{newGuildUser.Id} | {randomNumber}] User successfully removed from role");
                                }
                                catch (Exception ex)
                                {
                                    logger.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to remove user from role, message was: {ex.Message}\r\n{ex.StackTrace}");
                                }

                                // Cleanup the role if there is no one left in it
                                if (!foundRole.Members.Any())
                                {
                                    //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Role has no users left inside it, attempting to remove the role");
                                    try
                                    {
                                        await foundRole.DeleteAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to remove the role, message was: {ex.Message}\r\n{ex.StackTrace}");
                                    }
                                }
                            }

                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Removing user from role dictionary");
                            _roleMemory.Remove(foundMemory);
                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Process complete");
                        }
                        catch (Exception ex)
                        {
                            logger.Log($"[{newGuildUser.Id} | {randomNumber}] Unhandled exception for this event, message follows: {ex.Message}\r\n{ex.StackTrace}");
                        }
                    }
                    else if (newGuildUser.Activity is CustomStatusGame)
                    {
                        if (newGuildUser.Activity is Game game)
                        {
                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} has started an activity {newGuildUser.Activity.Name}, checking to see if it's in the game watcher database");

                            if (!DatabaseHandler.Instance.Exists(game.Name))
                            {
                                return;
                            }

                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Game is in db, processing...");

                            var roles = socketGuild.Roles;
                            var gameName = $"Playing: {game.Name}";

                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Checking to see if the user is already in a role named {gameName}");

                            // Check to see if this user is already part of the role, if they are ignore this.
                            if (newGuildUser.Roles.Any(x => x.Name.Equals(gameName))) return;

                            //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Checking to see if the role with the name of {gameName} exists within the guild");

                            // Does this role exist?
                            if (roles.Any(x => x.Name == gameName))
                            {
                                // Get the role, and add the user to it
                                var foundRole = roles.First(x => x.Name.Equals(gameName));

                                //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Found an existing role, adding the user to the role now");
                                try
                                {
                                    await newGuildUser.AddRoleAsync(foundRole);
                                    _roleMemory.Add(new GameRoleMemory(foundRole.Id, newGuildUser.Id)); // Remember this
                                }
                                catch (Exception ex)
                                {
                                    logger.Log($"[{newGuildUser.Id} | {randomNumber}] Exception while attempting to add the user to a role, message follows: {ex.Message}\r\n{ex.StackTrace}");
                                }
                            }
                            else
                            {
                                //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Role not found, will attempt to create it");

                                // Create the new role, and add the user to it
                                try
                                {
                                    var newRole = await socketGuild.CreateRoleAsync(gameName, isHoisted: true,
                                        color: Color.Red);

                                    //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Role created, attempting to modify its position");

                                    var botUser = newGuildUser.Guild.CurrentUser;
                                    var botRoles = botUser.Roles.OrderByDescending(x => x.Position);

                                    await newRole.ModifyAsync(properties => properties.Position = botRoles.First().Position - 1);

                                    //logger.Log($"[{newGuildUser.Id} | {randomNumber}] Position modified successfully, adding user to the role and saving memory.");

                                    await newGuildUser.AddRoleAsync(newRole);
                                    _roleMemory.Add(new GameRoleMemory(newRole.Id, newGuildUser.Id));
                                }
                                catch (Exception ex)
                                {
                                    logger.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to create or modify role, error is: {ex.Message}\r\n{ex.StackTrace}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
            finally
            {
                if (!skipWait)
                    _semaphoreSlim.Release();
            }
        }
    }
}