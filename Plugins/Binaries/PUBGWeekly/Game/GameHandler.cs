﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Discord;
using Discord.WebSocket;
using GlobalLogger;
using Pubg.Net;
using PUBGWeekly.Configuration;

namespace PUBGWeekly.Game
{
    public class GameHandler
    {
        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());
        private static readonly GameHandler _instance;

        private readonly List<Player> _players = new List<Player>();

        public DiscordSocketClient DiscordSocketClient;
        public bool IsLive;

        public GameHandler()
        {
            PubgWatcher.Instance.OnPubgGameEnded += (instance, gameData) => { OutputGameInfo(gameData); };
        }

        public void NewPlayer(string name, ulong discordId)
        {
            // Prevent a player from registering twice.
            var foundPlayer = _players.DefaultIfEmpty(null)
                .FirstOrDefault(x => x != null && x.DiscordId.Equals(discordId));
            if (foundPlayer != null) throw new ExceptionOverloads.PlayerAlreadyRegistered();

            _players.Add(new Player(name, discordId));
        }

        public void RemovePlayer(ulong discordId)
        {
            var foundPlayer = _players.DefaultIfEmpty(null)
                .FirstOrDefault(x => x != null && x.DiscordId.Equals(discordId));
            if (foundPlayer == null) throw new ExceptionOverloads.PlayerNotFound();

            _players.Remove(foundPlayer);
        }

        public void MovePlayersToTeamChannels()
        {
            var guild = DiscordSocketClient.GetGuild((ulong) PluginConfigurator.Instance.Configuration.GuildId);

            SendStatusMessage(
                "Moving all lobby players to their team channels, Please wait this can take a little while");

            foreach (var player in _players)
                try
                {
                    var teamId = player.Team;
                    var channelForTeam = PluginConfigurator.Instance.GetTeamChannel(teamId);

                    if (channelForTeam == 0) return;

                    MovePlayerToVoiceChannel(player.DiscordId, channelForTeam);

                    Thread.Sleep(750);
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log(
                        "[PubgWeekly-MovePlayersToTeamChannels] Exception while attempting to move players",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }

            SendStatusMessage("All players were moved (at least, they should have been!)");
            PubgWatcher.Instance.Start();
            SendStatusMessage(
                "PUBG API Watcher has started for this game, Will report match stats when the API allows it.");
        }

        public void OutputGameInfo(PubgMatch gameData)
        {
            var t = TimeSpan.FromSeconds(gameData.Duration);
            var duration = t.ToString(@"\:mm\:ss\:fff");

            var embedBuilder = new EmbedBuilder
            {
                Title = "New PUBG Game Detected",
                Description = $"Found {gameData.Rosters.Count()} Teams",
                ThumbnailUrl =
                    "https://media.discordapp.net/attachments/490303333479874562/713320093588914216/5667059_0.png",
                Author = new EmbedAuthorBuilder {Name = "PUBG Weekly Bot"},
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Map: {gameData.MapName} | Duration: {duration} | Teams: {gameData.Rosters.Count()}"
                },
                Color = new Color(255, 0, 0)
            };

            var teamCounter = 1;
            foreach (var roster in gameData.Rosters)
            {
                var fieldValue = "";
                foreach (var player in roster.Participants)
                    fieldValue +=
                        $"> {player.Stats.Name}\r\n> Kills:{player.Stats.Kills}\r\n> DBNO's: {player.Stats.DBNOs}\r\n> Dmg: {player.Stats.DamageDealt}\r\r";
                embedBuilder.AddField($"\r\n\r\nTeam {teamCounter}", fieldValue, true);
                teamCounter++;
            }

            SendStatusMessage("", embedBuilder.Build());
        }

        public async void SendStatusMessage(string msg, Embed embed = null)
        {
            var statusChannelId = PluginConfigurator.Instance.Configuration.StatusChannel;
            if (statusChannelId != 0)
            {
                var guild = DiscordSocketClient.GetGuild((ulong) PluginConfigurator.Instance.Configuration.GuildId);
                var channel = guild.GetTextChannel((ulong) PluginConfigurator.Instance.Configuration.StatusChannel);

                if (embed != null)
                {
                    await channel.SendMessageAsync(embed: embed);
                    return;
                }

                await channel.SendMessageAsync($"```csharp\r\n### PUBG Weekly System Message ###```>{msg}");
            }
        }

        public void MovePlayersToLobby()
        {
        }

        private async void MovePlayerToVoiceChannel(ulong playerId, ulong voiceChannelId)
        {
            var guild = DiscordSocketClient.GetGuild((ulong) PluginConfigurator.Instance.Configuration.GuildId);
            var user = guild.GetUser(playerId);
            var channel = guild.GetVoiceChannel(voiceChannelId);

            if (user.VoiceChannel != null)
                try
                {
                    await user.ModifyAsync(properties => properties.Channel = channel);
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log(
                        "[PubgWeekly-MovePlayerToVoiceChannel] Exception while moving player to voice channel",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
        }

        public void AssignTeams(int playersPerTeam)
        {
            // Reset the teams
            foreach (var player in _players)
                player.Team = 0;

            var rng = new Random(DateTime.Now.Millisecond);
            var loopAmount = Math.Ceiling((double) _players.Count / playersPerTeam);

            // Loop the amount of times we have players / per team  
            for (var i = 1; i <= loopAmount; i++)
            {
                // Create a team
                var unassignedPlayers = _players.Where(x => x.Team == 0);
                for (var j = 1; j <= playersPerTeam; j++)
                {
                    var rngPicked = rng.Next(unassignedPlayers.Count());
                    unassignedPlayers.ElementAt(rngPicked).Team = i;

                    // If we only have one player left, then that must be it - no matter the team size requirement.
                    if (unassignedPlayers.Count() == 0) break;
                }
            }
        }

        public void CreateNewGame()
        {
            // Clear the players.
            _players.Clear();
            IsLive = true;
        }

        public void StopGame()
        {
            _players.Clear();
            IsLive = false;
            PubgWatcher.Instance.Stop();
        }

        internal double TotalPlayerCount()
        {
            return _players.Count();
        }

        internal List<Player> GetPlayersInTeam(int i)
        {
            return _players.Where(x => x != null && x.Team == i).ToList();
        }

        internal List<Player> GetPlayers()
        {
            return _players;
        }
    }
}