using Interface;
using PluginManager;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GlobalLogger;
using Newtonsoft.Json;
using PUBGWeekly.Configuration.JSON;

namespace PUBGWeekly
{
    [Export(typeof(IPlugin))]
    public class PubgWeeklyPlugin : IPluginWithRouter
    {
        public PluginRouter PluginRouter { get; set; }

        public string Name => "PUBGWeekly";

        public string Description => "AGN PUBG Weekly Helper Plugin - Organise Teams etc";

        public EventRouter EventRouter { get; set; }

        public void Dispose()
        {
            
        }

        public void ExecutePlugin()
        {
            if (!PubgAPIConfigHandler.Instance.InitJsonConfig())
            {
                return;
            }

            Game.GameHandler.Instance.DiscordSocketClient = EventRouter.GetDiscordSocketClient();

            CommandHandler.HandlerManager.Instance.RegisterHandler<AdminCommandHandler>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<UserCommandHandler>();

            var db = InternalDatabase.Handler.Instance.GetConnection();
            db.RegisterTable<Configuration.SQL.Configuration>();
            db.RegisterTable<Configuration.SQL.TeamChannels>();
            db.RegisterTable<Configuration.SQL.PubgAccountLink>();

            Configuration.PluginConfigurator.Instance.LoadConfiguration();
            Configuration.PubgToDiscordManager.Instance.Load();

            EventRouter.GuildMemberUpdated += EventRouter_GuildMemberUpdated;
            EventRouter.UserVoiceStateUpdated += EventRouter_UserVoiceStateUpdated;
        }

        private Task EventRouter_UserVoiceStateUpdated(Discord.WebSocket.SocketUser sktUser, Discord.WebSocket.SocketVoiceState voiceStateOld, Discord.WebSocket.SocketVoiceState voiceStateNew)
        {
            if (voiceStateOld.VoiceChannel != null && voiceStateNew.VoiceChannel != null && voiceStateOld.VoiceChannel.Id == voiceStateNew.VoiceChannel.Id) return Task.CompletedTask;

            if ( voiceStateNew.VoiceChannel != null )
            {
                if (Game.GameHandler.Instance.IsLive)
                {
                    if (voiceStateNew.VoiceChannel.Id == (ulong)Configuration.PluginConfigurator.Instance.Configuration.LobbyId)
                    {
                        if (!Game.GameHandler.Instance.GetPlayers().Any(x => x != null && x.DiscordId == sktUser.Id))
                        {
                            // The new player is not part of pubg weekly, notify them to join up
                            Game.GameHandler.Instance.SendStatusMessage($"Hey {sktUser.Mention}, You can join PUBG Weekly by typing `!pubg join` into this channel!");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task EventRouter_GuildMemberUpdated(Discord.WebSocket.SocketGuildUser oldUser, Discord.WebSocket.SocketGuildUser newUser)
        {
            if ( newUser.Status == Discord.UserStatus.Offline )
            {
                if (Game.GameHandler.Instance.IsLive)
                {
                    try
                    {
                        Game.GameHandler.Instance.RemovePlayer(newUser.Id);
                        Game.GameHandler.Instance.SendStatusMessage($"Player {newUser.Username} has been removed from PUBG Weekly as they went offline");
                    }
                    catch (Exception)
                    { }
                }
            }

            return Task.CompletedTask;
        }
    }
}
