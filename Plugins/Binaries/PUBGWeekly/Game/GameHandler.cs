using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGWeekly.Game
{
    public class GameHandler
    {

        public static GameHandler Instance = _instance ?? (_instance = new GameHandler());
        private static GameHandler _instance;

        public Discord.WebSocket.DiscordSocketClient DiscordSocketClient;

        private List<Player> _players = new List<Player>();
        public bool IsLive = false;

        public void NewPlayer(string name, ulong discordId)
        {

            // Prevent a player from registering twice.
            var foundPlayer = _players.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.DiscordId.Equals(discordId));
            if (foundPlayer != null) throw new ExceptionOverloads.PlayerAlreadyRegistered();

            _players.Add(new Player(name, discordId));
        }

        public void RemovePlayer(ulong discordId)
        {
            var foundPlayer = _players.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.DiscordId.Equals(discordId));
            if (foundPlayer == null) throw new ExceptionOverloads.PlayerNotFound();

            _players.Remove(foundPlayer);
        }

        public void MovePlayersToTeamChannels()
        {
            var guild = DiscordSocketClient.GetGuild((ulong)Configuration.PluginConfigurator.Instance.Configuration.GuildId);

            SendStatusMessage("Moving all lobby players to their team channels, Please wait this can take a little while");

            foreach ( var player in _players )
            {
                try
                {
                    var teamId = player.Team;
                    var channelForTeam = Configuration.PluginConfigurator.Instance.GetTeamChannel(teamId);

                    if (channelForTeam == 0) return;

                    MovePlayerToVoiceChannel(player.DiscordId, channelForTeam);

                    System.Threading.Thread.Sleep(750);

                } catch (Exception)
                {

                }
            }

            SendStatusMessage("All players were moved (at least, they should have been!)");
        }

        public async void SendStatusMessage(string msg)
        {
#if DEBUG
            // Skip sending status messages in debug mode.
            return;
#endif

            var statusChannelId = Configuration.PluginConfigurator.Instance.Configuration.StatusChannel;
            if ( statusChannelId != 0 )
            {
                var guild = DiscordSocketClient.GetGuild((ulong)Configuration.PluginConfigurator.Instance.Configuration.GuildId);
                var channel = guild.GetTextChannel((ulong)Configuration.PluginConfigurator.Instance.Configuration.StatusChannel);

                await channel.SendMessageAsync($"```csharp\r\n### PUBG Weekly System Message ###```>{msg}");
            }
        }

        public void MovePlayersToLobby()
        {

        }

        private async void MovePlayerToVoiceChannel(ulong playerId, ulong voiceChannelId)
        {
            var guild = DiscordSocketClient.GetGuild((ulong)Configuration.PluginConfigurator.Instance.Configuration.GuildId);
            var user = guild.GetUser(playerId);
            var channel = guild.GetVoiceChannel(voiceChannelId);

            if (user.VoiceChannel != null)
            {
                try
                {
                    await user.ModifyAsync(properties => properties.Channel = channel);
                }
                catch (Exception)
                { }
            }
        }

        public void AssignTeams(int playersPerTeam)
        {

            // Reset the teams
            foreach (var player in _players)
                player.Team = 0;

            var rng = new Random(DateTime.Now.Millisecond);
            var loopAmount = Math.Ceiling((double)_players.Count / playersPerTeam);

            // Loop the amount of times we have players / per team  
            for ( var i=1; i <= loopAmount; i++)
            {
                // Create a team
                var unassignedPlayers = _players.Where(x => x.Team == 0);
                for ( var j=1; j <= playersPerTeam; j++ )
                {
                    var rngPicked = rng.Next(unassignedPlayers.Count());
                    unassignedPlayers.ElementAt(rngPicked).Team = i;

                    // If we only have one player left, then that must be it - no matter the team size requirement.
                    if (unassignedPlayers.Count() == 0)
                    {
                        break;
                    }
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
