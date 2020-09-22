using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameWatcher
{
    internal class GameHandler
    {
        private class GameRoleMemory
        {
            public ulong RoleId { get; }
            public ulong UserId { get; }

            public GameRoleMemory(ulong roleId, ulong userId)
            {
                RoleId = roleId;
                UserId = userId;
            }
        }

        // ReSharper disable once InconsistentNaming
        private static readonly GameHandler _instance;
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());
        
        private readonly List<GameRoleMemory> _roleMemory = new List<GameRoleMemory>();
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public PluginRouter PluginRouter { get; set; }
        public bool RolesReady;
        
        public async void StartGameWatcherTimer()
        {
            try
            {
                foreach (var guild in DiscordSocketClient.Guilds)
                {
                    if (!PluginRouter.IsPluginExecutableOnGuild(guild.Id)) continue;

                    foreach (var role in guild.Roles.Where(x => x.Name.StartsWith("Playing:")))
                    {
                        GlobalLogger.Log4NetHandler.Log($"[GameWatcher] Found a role called {role.Name}, it will be deleted for cleanup.", GlobalLogger.Log4NetHandler.LogLevel.INFO);
                        await role.DeleteAsync();
                    }
                }

                RolesReady = true;
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log("Game2Role Unhandled Exception", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }
        
        public async Task GameScan(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser)
        {
            // When the bot starts up, we have to clean the roles on the guilds.
            // we make sure that task is done before we accept new calls.
            if (!RolesReady) return;
            if (newGuildUser.IsBot) return;

            if (newGuildUser.Activity != null)
            {
                if (newGuildUser.Activity.Name.Equals("Custom Status") || newGuildUser.Activity.Name.Equals("Spotify"))
                    return;
            }

            var random = new Random(DateTime.Now.Millisecond);
            var randomNumber = random.Next(1000, 10000);
            try
            {
                // Grab the guild
                if (newGuildUser.Guild is SocketGuild socketGuild)
                {
                    if (!(newGuildUser.Activity is Game) && oldGuildUser != null)
                    {
                        var foundMemory = _roleMemory?.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.UserId == newGuildUser.Id && newGuildUser.Roles.Any(y => y.Id == x.RoleId));
                        if (foundMemory == null)
                            return;

                        try
                        {
                            GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} had an activity of {oldGuildUser.Activity?.Name} but now doesn't - attempting to find role", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                            var foundRole = socketGuild.GetRole(foundMemory.RoleId);

                            if (foundRole != null)
                            {
                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Role found, attempting to remove user {newGuildUser.Username} from role", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                try
                                {
                                    await newGuildUser.RemoveRoleAsync(foundRole);
                                    GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} successfully removed from role", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                }
                                catch (Exception ex)
                                {
                                    GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to remove user {newGuildUser.Username} from role, message was: {ex.Message}\r\n{ex.StackTrace}", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                }

                                // Cleanup the role if there is no one left in it
                                if (!foundRole.Members.Any())
                                {
                                    GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Role has no users left inside it, attempting to remove the role", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                    try
                                    {
                                        await foundRole.DeleteAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to remove the role, message was: {ex.Message}\r\n{ex.StackTrace}", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                    }
                                }
                            }
                            _roleMemory.Remove(foundMemory);
                        }
                        catch (Exception ex)
                        {
                            GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Unhandled exception for this event.", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception: ex);
                        }
                    }
                    else if (newGuildUser.Activity is Game game)
                    {
                        // Check to see if the old user and new user are playing the same game
                        // This happens in games that interact with discord directly, eg: PUBG updates discord.
                        if (oldGuildUser != null && oldGuildUser.Activity is Game gameOldUser)
                        {
                            if (gameOldUser.Name.Equals(game.Name))
                            {
                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} is already playing {game.Name}, Skipping! ", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                                return;
                            }
                        }

                        GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] User {newGuildUser.Username} has started an activity {newGuildUser.Activity.Name}, checking to see if it's in the game watcher database", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                        if (!DatabaseHandler.Instance.Exists(game.Name))
                        {
                            return;
                        }

                        GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Game is in db, processing...", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                        var roles = socketGuild.Roles;
                        var gameName = $"Playing: {game.Name}";

                        GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Checking to see if the user {newGuildUser.Username} is already in a role named {gameName}", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                        // Check to see if this user is already part of the role, if they are ignore this.
                        if (newGuildUser.Roles.Any(x => x.Name.Equals(gameName))) return;

                        GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Checking to see if the role with the name of {gameName} exists within the guild {socketGuild.Name}", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                        // Does this role exist?
                        if (roles.Any(x => x.Name == gameName))
                        {
                            // Get the role, and add the user to it
                            var foundRole = roles.First(x => x.Name.Equals(gameName));

                            GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Found an existing role, adding the user {newGuildUser.Username} to the role now", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);
                            try
                            {
                                await newGuildUser.AddRoleAsync(foundRole, new RequestOptions(){RetryMode = RetryMode.AlwaysFail});
                                _roleMemory.Add(new GameRoleMemory(foundRole.Id, newGuildUser.Id)); // Remember this
                            }
                            catch (Exception ex)
                            {
                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Exception while attempting to add the user {newGuildUser.Username} to a role.", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception: ex);
                            }
                        }
                        else
                        {
                            GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Role not found, will attempt to create it", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                            // Create the new role, and add the user to it
                            try
                            {
                                var newRole = await socketGuild.CreateRoleAsync(gameName,null, options:new RequestOptions() { RetryMode = RetryMode.AlwaysFail }, isHoisted: true, color: Color.Red, isMentionable: false);

                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Role created, attempting to modify its position", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                                var botUser = newGuildUser.Guild.CurrentUser;
                                var botRoles = botUser.Roles.OrderByDescending(x => x.Position);

                                await newRole.ModifyAsync(properties => properties.Position = botRoles.First().Position - 1);

                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Position modified successfully, adding user {newGuildUser.Username} to the role and saving memory.", GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

                                await newGuildUser.AddRoleAsync(newRole, new RequestOptions(){RetryMode = RetryMode.AlwaysFail});
                                _roleMemory.Add(new GameRoleMemory(newRole.Id, newGuildUser.Id));
                            }
                            catch (Exception ex)
                            {
                                GlobalLogger.Log4NetHandler.Log($"[{newGuildUser.Id} | {randomNumber}] Unable to create or modify role.", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception: ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log("Game2Role Unhandled Exception", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }
    }
}