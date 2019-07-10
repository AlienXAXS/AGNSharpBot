using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger.AdvancedLogger;

namespace GameWatcher
{
    class GameHandler
    {
        private static GameHandler _instance;
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Dictionary<ulong, ulong> _roleMemory = new Dictionary<ulong, ulong>();

        public Dictionary<ulong, ulong> GetMemory()
        {
            return _roleMemory;
        }

        public async Task GameScan(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser)
        {
            var logger = AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);
            logger.Log($"[{newGuildUser.Id}] User {newGuildUser.Username} has fired the GameScan method");

            try
            {
                await _semaphoreSlim.WaitAsync();
                
                // Grab the guild
                if (newGuildUser.Guild is SocketGuild socketGuild)
                {
                    // Check to see if the activity is now nothing (aka, the user quit their app)
                    if (newGuildUser.Activity?.Type != ActivityType.Playing)
                    {
                        var foundMemory = _roleMemory?.FirstOrDefault(x => x.Key == newGuildUser.Id);
                        if (foundMemory != null && foundMemory.Value.Key == 0)
                            return;

                        try
                        {

                            logger.Log($"[{newGuildUser.Id}] User {newGuildUser.Username} had an activity of {oldGuildUser.Activity.Name} but now doesnt - attempting to find role");

                            var foundRole = socketGuild.Roles.DefaultIfEmpty(null).FirstOrDefault(x => x.Id == foundMemory.Value.Value);

                            if (foundRole != null)
                            {
                                logger.Log($"[{newGuildUser.Id}] Role found, attempting to remove user from role");
                                try
                                {
                                    await newGuildUser.RemoveRoleAsync(foundRole);
                                    logger.Log($"[{newGuildUser.Id}] User successfully removed from role");
                                }
                                catch (Exception ex)
                                {
                                    logger.Log($"[{newGuildUser.Id}] Unable to remove user from role, message was: {ex.Message}\r\n{ex.StackTrace}");
                                }

                                // Cleanup the role if there is no one left in it
                                if (!foundRole.Members.Any())
                                {
                                    logger.Log($"[{newGuildUser.Id}] Role has no users left inside it, attempting to remove the role");
                                    try
                                    {
                                        await foundRole.DeleteAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Log($"[{newGuildUser.Id}] Unable to remove the role, message was: {ex.Message}\r\n{ex.StackTrace}");
                                    }
                                }
                            }

                            logger.Log($"[{newGuildUser.Id}] Removing user from role dictionary");
                            _roleMemory.Remove(newGuildUser.Id);
                            logger.Log($"[{newGuildUser.Id}] Process complete");
                        }
                        catch (Exception ex)
                        {
                            logger.Log($"[{newGuildUser.Id}] Unhandled exception for this event, message follows: {ex.Message}\r\n{ex.StackTrace}");
                        }
                    }
                    else if(newGuildUser.Activity?.Type == ActivityType.Playing)
                    {
                        if (newGuildUser.Activity is Game game && newGuildUser.Activity.Type == ActivityType.Playing)
                        {

                            logger.Log($"[{newGuildUser.Id}] User {newGuildUser.Username} has started an activity {newGuildUser.Activity.Name}, checking to see if it's in the game watcher database");

                            if (!DatabaseHandler.Instance.Exists(game.Name)) return;

                            logger.Log($"[{newGuildUser.Id}] Game is in db, processing...");

                            var roles = socketGuild.Roles;
                            var gameName = $"Playing: {game.Name}";

                            logger.Log($"[{newGuildUser.Id}] Checking to see if the user is already in a role named {gameName}");

                            // Check to see if this user is already part of the role, if they are ignore this.
                            if (newGuildUser.Roles.Any(x => x.Name.Equals(gameName))) return;

                            logger.Log($"[{newGuildUser.Id}] Checking to see if the role with the name of {gameName} exists within the guild");

                            // Does this role exist?
                            if (roles.Any(x => x.Name == gameName))
                            {
                                // Get the role, and add the user to it
                                var foundRole = roles.First(x => x.Name.Equals(gameName));

                                logger.Log(
                                    $"[{newGuildUser.Id}] Found an existing role, adding the user to the role now");
                                try
                                {
                                    await newGuildUser.AddRoleAsync(foundRole);
                                    _roleMemory.Add(newGuildUser.Id, foundRole.Id); // Remember this
                                }
                                catch (Exception ex)
                                {
                                    logger.Log(
                                        $"[{newGuildUser.Id}] Exception while attempting to add the user to a role, message follows: {ex.Message}\r\n{ex.StackTrace}");
                                }
                            }
                            else
                            {
                                logger.Log($"[{newGuildUser.Id}] Role not found, will attempt to create it");

                                // Create the new role, and add the user to it
                                try
                                {
                                    var newRole = await socketGuild.CreateRoleAsync(gameName, isHoisted: true,
                                        color: Color.Red);

                                    logger.Log(
                                        $"[{newGuildUser.Id}] Role created, attempting to modify its position");

                                    await newRole.ModifyAsync(properties =>
                                        properties.Position = socketGuild.Roles.Count - 2);

                                    logger.Log(
                                        $"[{newGuildUser.Id}] Position modified successfully, adding user to the role and saving memory.");

                                    await newGuildUser.AddRoleAsync(newRole);
                                    _roleMemory.Add(newGuildUser.Id, newRole.Id);
                                }
                                catch (Exception ex)
                                {
                                    logger.Log(
                                        $"[{newGuildUser.Id}] Unable to create or modify role, error is: {ex.Message}\r\n{ex.StackTrace}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}