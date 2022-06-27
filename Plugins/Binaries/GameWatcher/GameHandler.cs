using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger;
using PluginManager;

namespace GameWatcher
{
    internal class GameHandler : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private static readonly GameHandler _instance;
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());

        private readonly List<GameRoleMemory> _roleMemory = new List<GameRoleMemory>();

        private bool _runScanThread = true;
        public bool RolesReady;
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public PluginRouter PluginRouter { get; set; }

        public void Dispose()
        {
            _runScanThread = false;
        }

        public async void StartGameWatcherTimer()
        {
            try
            {
                foreach (var guild in DiscordSocketClient.Guilds)
                {
                    if (!PluginRouter.IsPluginExecutableOnGuild(guild.Id)) continue;

                    foreach (var role in guild.Roles.Where(x => x.Name.StartsWith("Playing:")))
                    {
                        Log4NetHandler.Log(
                            $"[GameWatcher] Found a role called {role.Name}, it will be deleted for cleanup.",
                            Log4NetHandler.LogLevel.INFO);
                        try
                        {
                            await role.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            Log4NetHandler.Log($"Unable to remove role {role.Name} in {role.Guild.Name}", Log4NetHandler.LogLevel.ERROR, exception:ex);
                        }
                    }
                }

                RolesReady = true;
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("Game2Role Unhandled Exception", Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }

        public async Task GameScan(SocketGuildUser newGuildUser, SocketPresence oldPresence, SocketPresence newPresence)
        {
            // When the bot starts up, we have to clean the roles on the guilds.
            // we make sure that task is done before we accept new calls.
            if (!RolesReady) return;
            if (newGuildUser.IsBot) return;

            var newGameActivity = (bool) newPresence?.Activities.Any()
                ? (Game) newPresence?.Activities.DefaultIfEmpty(null).FirstOrDefault(x =>
                    x.Type == ActivityType.Playing || x.Type == ActivityType.Streaming)
                : null;

            var oldGameActivity = (bool) oldPresence?.Activities.Any()
                ? (Game) oldPresence?.Activities.DefaultIfEmpty(null).FirstOrDefault(x =>
                    x.Type == ActivityType.Playing || x.Type == ActivityType.Streaming)
                : null;
            
            //if ( !oldPresence.Activities.Any() && !newPresence.Activities.Any() )
            //    newGameActivity = (Game)newGuildUser.Activities.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.Type == ActivityType.Playing || x.Type == ActivityType.Streaming);


            var randomNumber = Thread.CurrentThread.ManagedThreadId;
            try
            {
                // Grab the guild
                var socketGuild = newGuildUser.Guild;

                // We had an old game activity, but now we do not, need to remove.
                if (oldGameActivity != null && newGameActivity == null)
                {
                    var foundMemory = _roleMemory?.DefaultIfEmpty(null).FirstOrDefault(x =>
                        x != null && x.UserId == newGuildUser.Id && newGuildUser.Roles.Any(y => y.Id == x.RoleId));
                    if (foundMemory == null)
                        return;

                    try
                    {

                        var foundRole = socketGuild.GetRole(foundMemory.RoleId);

                        if (foundRole != null)
                        {
                            try
                            {
                                await newGuildUser.RemoveRoleAsync(foundRole);
                            }
                            catch (Exception ex)
                            {
                                Log4NetHandler.Log(
                                    $"[{newGuildUser.Id} | {randomNumber}] Unable to remove user {newGuildUser.Username} from role, message was: {ex.Message}\r\n{ex.StackTrace}",
                                    Log4NetHandler.LogLevel.DEBUG);
                            }

                            // Cleanup the role if there is no one left in it
                            if (!foundRole.Members.Any())
                            {
                                try
                                {
                                    await foundRole.DeleteAsync();
                                }
                                catch (Exception ex)
                                {
                                    Log4NetHandler.Log(
                                        $"[{newGuildUser.Id} | {randomNumber}] Unable to remove the role, message was: {ex.Message}\r\n{ex.StackTrace}",
                                        Log4NetHandler.LogLevel.DEBUG);
                                }
                            }
                        }

                        _roleMemory.Remove(foundMemory);
                    }
                    catch (Exception ex)
                    {
                        Log4NetHandler.Log(
                            $"[{newGuildUser.Id} | {randomNumber}] Unhandled exception for this event.",
                            Log4NetHandler.LogLevel.ERROR, exception: ex);
                    }
                }
                // We have a user that has a new game activity.
                else if (newGameActivity != null)
                {
                    // Check to see if the old user and new user are playing the same game
                    // This happens in games that interact with discord directly, eg: PUBG updates discord.
                    if (oldGameActivity != null && oldGameActivity.Name.Equals(newGameActivity.Name))
                    {
                        return;
                    }

                    if (!DatabaseHandler.Instance.Exists(newGameActivity.Name, socketGuild.Id, true)) return;

                    var roles = socketGuild.Roles;
                    var gameName = $"Playing: {newGameActivity.Name}";

                    // Check to see if this user is already part of the role, if they are ignore this.
                    if (newGuildUser.Roles.Any(x => x.Name.Equals(gameName))) return;

                    // Does this role exist?
                    if (roles.Any(x => x.Name.Equals(gameName)))
                    {
                        // Get the role, and add the user to it
                        var foundRole = roles.First(x => x.Name.Equals(gameName));

                        try
                        {
                            await newGuildUser.AddRoleAsync(foundRole,
                                new RequestOptions {RetryMode = RetryMode.AlwaysFail});
                            _roleMemory.Add(new GameRoleMemory(foundRole.Id, newGuildUser.Id));
                        }
                        catch (Exception ex)
                        {
                            Log4NetHandler.Log(
                                $"[{newGuildUser.Id} | {randomNumber}] Exception while attempting to add the user {newGuildUser.Username} to a role.",
                                Log4NetHandler.LogLevel.ERROR, exception: ex);
                        }
                    }
                    else
                    {

                        // Create the new role, and add the user to it
                        try
                        {
                            var newRole = await socketGuild.CreateRoleAsync(gameName, null,
                                options: new RequestOptions {RetryMode = RetryMode.AlwaysFail}, isHoisted: true,
                                color: Color.Red, isMentionable: false);
                            
                            var botUser = newGuildUser.Guild.CurrentUser;
                            var botRoles = botUser.Roles.OrderByDescending(x => x.Position);

                            await newRole.ModifyAsync(properties =>
                                properties.Position = botRoles.First().Position - 1);
                            
                            await newGuildUser.AddRoleAsync(newRole,
                                new RequestOptions {RetryMode = RetryMode.AlwaysFail});
                            _roleMemory.Add(new GameRoleMemory(newRole.Id, newGuildUser.Id));
                        }
                        catch (Exception ex)
                        {
                            Log4NetHandler.Log(
                                $"[{newGuildUser.Id} | {randomNumber}] Unable to create or modify role.",
                                Log4NetHandler.LogLevel.ERROR, exception: ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("Game2Role Unhandled Exception", Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }

        private class GameRoleMemory
        {
            public GameRoleMemory(ulong roleId, ulong userId)
            {
                RoleId = roleId;
                UserId = userId;
            }

            public ulong RoleId { get; }
            public ulong UserId { get; }
        }
    }
}