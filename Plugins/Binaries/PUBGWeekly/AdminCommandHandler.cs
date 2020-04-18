using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace PUBGWeekly
{
    class AdminCommandHandler
    {
        [Command("pubgweeklyadmin", "The primary administrator command for PUBG Weekly Utiltiies - try !pubgweeklyadmin help")]
        [Alias("pgwa", "pubga")]
        public async void CommandHandler(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 0) return;

            try
            {
                switch (parameters[1].ToLower())
                {
                    case "help":

                        var helpText = "```PUBG Weekly Admin Command Help```\r\n" +
                            "`create`\r\n" +
                            "Creates a new PUBG Weekly Event, use this before anything else. (use create true to add lobby players automatically)\r\n" +
                            "\r\n" +
                            "`stop`\r\n" +
                            "Stops the current PUBG Weekly event, cleaning all teams for next time.\r\n" +
                            "\r\n\r\n" +

                            "``` Player Management```\r\n" +
                            "`add < player_id >`\r\n" +
                            "Adds a player to PUBG Weekly\r\n" +
                            "\r\n" +
                            "`remove < player_id >`\r\n" +
                            "Removes a player from PUBG Weekly\r\n" +
                            "\r\n" +
                            "`listplayers`\r\n" +
                            "Generates a nice list of players\r\n" +
                            "\r\n\r\n" +

                            "```Game Helpers```\r\n" +
                            "`assign < teamsize >`\r\n" +
                            "Mix players up into teams\r\n" +
                            "\r\n" +
                            "`move`\r\n" +
                            "Moves all players into their team channels\r\n" +
                            "\r\n" +
                            "`lobby`\r\n" +
                            "Moves all players into the lobby\r\n" +
                            "\r\n\r\n" +
                            "```Super Admin Stuff```" +
                            "`config` <category/lobby/team/status> <id> [channelid]\r\n" +
                            "Configures the bot to look at certain channels for certain things, eg: !pgwa voice category <id> | !pgwa voice lobby <id> | !pgwa voice team1-15 \"TEAM\"";

                        await sktMessage.Channel.SendMessageAsync(helpText);
                        break;

                    case "create":
                        Game.GameHandler.Instance.CreateNewGame();
                        if ( parameters.Length == 3 )
                        {
                            if ( parameters[2].ToLower() == "true" )
                            {
                                AddLobbyPlayers(sktMessage);
                            }
                        }
                        await sktMessage.Channel.SendMessageAsync("New PUBG Weekly game created, join this game by typing !pgw join");
                        break;

                    case "stop":
                        Game.GameHandler.Instance.StopGame();
                        await sktMessage.Channel.SendMessageAsync("PUBG Weekly game removed, all players and teams have been deleted.");
                        break;

                    case "remove":
                        if (parameters.Length == 3)
                            RemovePlayerFromGame(parameters[2], sktMessage);
                        break;

                    case "add":
                        if (parameters.Length == 3)
                            AddNewPlayerToGame(parameters[2], sktMessage);
                        else
                            await sktMessage.Channel.SendMessageAsync("Invalid syntax");
                        break;

                    case "listplayers":
                    case "list":
                    case "lp":
                        ListPlayers(sktMessage);
                        break;

                    case "assign":
                    case "mix":
                        if (parameters.Length == 3)
                        {
                            var teamSize = 0;
                            if (int.TryParse(parameters[2], out teamSize))
                                if (Game.GameHandler.Instance.IsLive)
                                {
                                    Game.GameHandler.Instance.AssignTeams(teamSize);

                                    var msg = "";
                                    var maxTeams = Math.Ceiling((double)Game.GameHandler.Instance.TotalPlayerCount() / teamSize);
                                    for ( var i = 1; i <= maxTeams; i++)
                                    {
                                        var foundPlayers = Game.GameHandler.Instance.GetPlayersInTeam(i);
                                        msg += $"```TEAM {i}```";
                                        foreach (var player in foundPlayers)
                                        {
                                            msg += player.Name + "\r\n";
                                        }
                                        msg += "\r\n";
                                    }

                                    await sktMessage.Channel.SendMessageAsync(msg);
                                }
                        }
                        break;

                    case "move":
                        Game.GameHandler.Instance.MovePlayersToTeamChannels();
                        break;

                    case "lobby":
                        Game.GameHandler.Instance.MovePlayersToLobby();
                        break;

                    case "add_lobby":
                        AddLobbyPlayers(sktMessage);
                        break;

                    case "config":
                        if (parameters.Length < 3) return;

                        if (sktMessage.Channel is SocketGuildChannel socketChannel)
                        {
                            switch (parameters[2].ToLower())
                            {
                                case "category":
                                    AssignRootCategory(parameters[3], sktMessage);
                                    break;

                                case "lobby":
                                    AssignLobby(parameters[3], sktMessage);
                                    break;

                                case "team":
                                    if (parameters.Length != 5)
                                    {
                                        await sktMessage.Channel.SendMessageAsync("Invalid syntax");
                                        return;
                                    }
                                    AssignTeamChannel(parameters[3], parameters[4], sktMessage);
                                    break;

                                case "guild-assign":
                                    AssignGuild(sktMessage);
                                    break;

                                case "check":
                                    PreformConfigCheck(sktMessage);
                                    break;

                                case "status":
                                    AssignStatusChannel(parameters[3], sktMessage);
                                    break;

                                default:
                                    await sktMessage.Channel.SendMessageAsync("Unknown admin command");
                                    break;

                            }
                        }
                    break;
                }
            } catch (Exception ex)
            {

            }
        }

        private async void RemovePlayerFromGame(string userid, SocketMessage sktMessage)
        {
            ulong playerId = 0;
            if (ulong.TryParse(userid, out playerId))
            {
                try
                {
                    Game.GameHandler.Instance.RemovePlayer(playerId);
                    await sktMessage.Channel.SendMessageAsync("User was removed from the PUBG Weekly Player Listing");
                }
                catch (ExceptionOverloads.PlayerNotFound)
                {
                    await sktMessage.Channel.SendMessageAsync("I can't find that user!");
                }
            }
        }

        private async void AddLobbyPlayers(SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel socketGuildChannel)
            {
                var guild = socketGuildChannel.Guild;
                var voiceChannelId = Configuration.PluginConfigurator.Instance.GetLobbyChannel();
                var channel = guild.GetVoiceChannel(voiceChannelId);
                var msg = "> Added the following members:";
                foreach ( var member in channel.Users)
                {
                    try
                    {
                        Game.GameHandler.Instance.NewPlayer(member.Username, member.Id);
                        msg += $"{member.Username}\r\n";
                    } catch (Exception)
                    {
                        // Ignore this exception, who cares.
                    }
                }

                await sktMessage.Channel.SendMessageAsync(msg);
            }
        }

        private async void ListPlayers(SocketMessage sktMessage)
        {
            if (!Game.GameHandler.Instance.IsLive)
            {
                return;
            }

            var msg = "```Players currently in this weeks PUBG Weekly\r\n\r\n";
            foreach (var player in Game.GameHandler.Instance.GetPlayers())
            {
                msg += $"{player.Name}\r\n";
            }

            msg += "```";
            await sktMessage.Channel.SendMessageAsync(msg);
        }

        private async void AddNewPlayerToGame(string strPlayerId, SocketMessage sktMessage)
        {
            if ( !Game.GameHandler.Instance.IsLive )
            {
                await sktMessage.Channel.SendMessageAsync("No PUBG Game is live currently");
                return;
            }

            ulong playerId = 0;
            if (ulong.TryParse(strPlayerId, out playerId))
            {
                if (sktMessage.Channel is SocketGuildChannel socketChannel)
                {
                    var user = socketChannel.Guild.GetUser(playerId);
                    if (user != null)
                    {
                        try
                        {
                            Game.GameHandler.Instance.NewPlayer(user.Username, user.Id);
                            await sktMessage.Channel.SendMessageAsync($"Added player {user.Username} to the PUBG Weekly Playerlist");
                        } catch ( ExceptionOverloads.PlayerAlreadyRegistered )
                        {
                            await sktMessage.Channel.SendMessageAsync($"Unable to add player {strPlayerId}, This person is already registered in this game.");
                        } catch (Exception ex)
                        {
                            await sktMessage.Channel.SendMessageAsync($"Unable to add player {strPlayerId}, Fatal Error: {ex.Message}\r\n{ex.StackTrace}");
                        }
                    } else
                    {
                        await sktMessage.Channel.SendMessageAsync($"Unable to add player {strPlayerId}, I cannot find a user with this Discord ID");
                    }
                }
            } else
            {
                await sktMessage.Channel.SendMessageAsync($"Unable to add player {strPlayerId}, unable to convert to Discord Player ID");
            }
        }

        private async void AssignStatusChannel(string strChannelId, SocketMessage sktMessage)
        {
            if (Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId == 0)
            {
                await sktMessage.Channel.SendMessageAsync($"Unable to assign Team Channel, the root category is missing - configure that first");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                ulong channelId = 0;
                if ( ulong.TryParse(strChannelId, out channelId) )
                {
                    var foundChannel = socketChannel.Guild.GetTextChannel(channelId);
                    if (foundChannel != null)
                    {
                        if ((long)foundChannel.CategoryId == Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId)
                        {
                            Configuration.PluginConfigurator.Instance.AssignStatusChannel(foundChannel.Id);
                            await sktMessage.Channel.SendMessageAsync($"Channel {foundChannel.Name} assigned to Status Messages Successfully.");
                        }
                        else
                        {
                            await sktMessage.Channel.SendMessageAsync($"Unable to assign channel {foundChannel.Name} as it's parent category isnt the configured root category");
                        }
                    }
                    else
                    {
                        await sktMessage.Channel.SendMessageAsync($"Cannot find a channel with the ID of {strChannelId}, are you sure you're using a Voice Channel ID?");
                    }
                }
            }
        }

        private async void AssignTeamChannel(string strTeamId, string strChannelId, SocketMessage sktMessage)
        {
            if ( Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId == 0 )
            {
                await sktMessage.Channel.SendMessageAsync($"Unable to assign Team Channel, the root category is missing - configure that first");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                int teamId = 0;
                ulong channelId = 0;
                if ( int.TryParse(strTeamId, out teamId) )
                {
                    if ( ulong.TryParse(strChannelId, out channelId) )
                    {
                        var foundChannel = socketChannel.Guild.GetVoiceChannel(channelId);
                        if ( foundChannel != null )
                        {
                            if ( (long)foundChannel.CategoryId == Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId )
                            {
                                Configuration.PluginConfigurator.Instance.AssignTeamChannel(teamId, foundChannel.Id);
                                await sktMessage.Channel.SendMessageAsync($"Channel {foundChannel.Name} assigned to Team {teamId} successfully.");
                            } else
                            {
                                await sktMessage.Channel.SendMessageAsync($"Unable to assign channel {foundChannel.Name} as it's parent category isnt the configured root category");
                            }
                        } else
                        {
                            await sktMessage.Channel.SendMessageAsync($"Cannot find a channel with the ID of {strChannelId}, are you sure you're using a Voice Channel ID?");
                        }
                    } else {
                        await sktMessage.Channel.SendMessageAsync($"The given Team Channel ID of {strChannelId} is invalid");
                    }
                } else {
                    await sktMessage.Channel.SendMessageAsync($"The given Team ID of {strTeamId} is invalid");
                }
            }
        }

        private async void AssignRootCategory(string category, SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                ulong catId = 0;
                if (!ulong.TryParse(category, out catId))
                {
                    await sktMessage.Channel.SendMessageAsync("Invalid syntax or channel ID");
                    return;
                }
                var foundCategory = socketChannel.Guild.GetCategoryChannel(catId);
                if (foundCategory == null)
                {
                    await sktMessage.Channel.SendMessageAsync("Invalid syntax or channel ID");
                    return;
                }

                Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId = (long)foundCategory.Id;
                Configuration.PluginConfigurator.Instance.SaveConfig();

                await sktMessage.Channel.SendMessageAsync($"Category `{foundCategory.Name}` has been assigned as the root catgetory");
            }
        }

        private async void AssignGuild(SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                Configuration.PluginConfigurator.Instance.Configuration.GuildId = (long)socketChannel.Guild.Id;
                Configuration.PluginConfigurator.Instance.SaveConfig();
                await sktMessage.Channel.SendMessageAsync("Successfully assigned guild to PUBG Weekly Events");
            }
        }

        private async void PreformConfigCheck(SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                var chk_rootCategory = socketChannel.Guild.GetCategoryChannel((ulong)Configuration.PluginConfigurator.Instance.Configuration.RootCategoryId);
                var chk_guildId = Configuration.PluginConfigurator.Instance.Configuration.GuildId;

                if (chk_rootCategory == null || chk_guildId == null)
                {
                    await sktMessage.Channel.SendMessageAsync("Config is invalid, missing either root channel or guild assignment");
                    return;
                }

                await sktMessage.Channel.SendMessageAsync($"> Configuration\r\n" +
                    $"Guild ID: {chk_guildId}\r\n" +
                    $"Root Category: {chk_rootCategory.Name}");
            }
        }

        private async void AssignLobby(string strLobbyId, SocketMessage sktMessage)
        {
            if (sktMessage.Channel is SocketGuildChannel socketChannel)
            {
                ulong lobbyId = 0;
                if (!ulong.TryParse(strLobbyId, out lobbyId))
                {
                    await sktMessage.Channel.SendMessageAsync("Invalid syntax or channel ID");
                    return;
                }
                var foundLobby = socketChannel.Guild.GetVoiceChannel(lobbyId);
                if (foundLobby == null)
                {
                    await sktMessage.Channel.SendMessageAsync("Invalid syntax or channel ID");
                    return;
                }

                Configuration.PluginConfigurator.Instance.Configuration.LobbyId = (long)foundLobby.Id;
                Configuration.PluginConfigurator.Instance.SaveConfig();

                await sktMessage.Channel.SendMessageAsync($"Category `{foundLobby.Name}` has been assigned as the staging voice channel lobby");
            }
        }
    }
}
