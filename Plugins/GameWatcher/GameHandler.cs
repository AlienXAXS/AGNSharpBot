using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger;

namespace GameWatcher
{
    class GameHandler
    {
        private static GameHandler _instance;
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());

        private readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Dictionary<ulong, ulong> _roleMemory = new Dictionary<ulong, ulong>();

        public async void GameScan(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser, bool firstRun = false)
        {
            try
            {
                // As we're dealing with adding/deleting roles, we should make this thread-safe to ensure roles exist before adding people to a role.
                // Reason for this, is if multiple people start a game at the same time, we must execute them one at a time.
                await SemaphoreSlim.WaitAsync();

                // Grab the guild
                if (newGuildUser.Guild is SocketGuild socketGuild)
                {
                    // Check to see if the activity is now nothing (aka, the user quit their app)
                    if (newGuildUser.Activity?.Type != ActivityType.Playing && !firstRun)
                    {
                        await Logger.Instance.Log(
                            $"[GAMEWATCHER] User {newGuildUser.Username} has stopped activity",
                            Logger.LoggerType.ConsoleOnly);

                        var foundMemory = _roleMemory?.FirstOrDefault(x => x.Key == newGuildUser.Id);
                        if (foundMemory != null && foundMemory.Value.Key == 0)
                            return;

                        await Logger.Instance.Log(
                            $"[GAMEWATCHER] Found key {foundMemory.Value.Key} for user {newGuildUser.Username} in memory, attempting to delete them from the role",
                            Logger.LoggerType.ConsoleOnly);

                        var foundRole = socketGuild.Roles.DefaultIfEmpty(null)
                            .FirstOrDefault(x => x.Id == foundMemory.Value.Value);

                        if (foundRole != null) await newGuildUser.RemoveRoleAsync(foundRole);

                        _roleMemory.Remove(newGuildUser.Id);
                    }
                    else if(newGuildUser.Activity?.Type == ActivityType.Playing)
                    {
                        if (newGuildUser.Activity is Game game && newGuildUser.Activity.Type == ActivityType.Playing)
                        {
                            if (!DatabaseHandler.Instance.Exists(game.Name)) return;

                            var roles = socketGuild.Roles;

                            var gameName = $"Playing: {game.Name}";

                            // Check to see if this user is already part of the role, if they are ignore this.
                            if (newGuildUser.Roles.Any(x => x.Name.Equals(gameName))) return;

                            // Does this role exist?
                            if (roles.Any(x => x.Name == gameName))
                            {
                                // Get the role, and add the user to it
                                var foundRole = roles.First(x => x.Name.Equals(gameName));
                                await newGuildUser.AddRoleAsync(foundRole);
                                _roleMemory.Add(newGuildUser.Id, foundRole.Id); // Remember this
                            }
                            else
                            {
                                // Create the new role, and add the user to it
                                var newRole =
                                    await socketGuild.CreateRoleAsync(gameName, isHoisted: true, color: Color.Red);
                                await newRole.ModifyAsync(properties =>
                                    properties.Position = socketGuild.Roles.Count - 1);

                                Debug.Print($"Role Position: {newRole.Position}");

                                await newGuildUser.AddRoleAsync(newRole);
                                _roleMemory.Add(newGuildUser.Id, newRole.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Logger.Instance.Log($"{ex.Message}\r\n\r\n{ex.StackTrace}", Logger.LoggerType.ConsoleOnly);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
    }
}
