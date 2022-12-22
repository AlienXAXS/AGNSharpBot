using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using Interface;
using InternalDatabase;
using PluginManager;
using PUBGWeekly.Configuration;
using PUBGWeekly.Configuration.JSON;
using PUBGWeekly.Configuration.SQL;
using PUBGWeekly.Game;

namespace PUBGWeekly
{
    [Export(typeof(IPlugin))]
    public class PubgWeeklyPlugin : IPluginWithRouter
    {
        public PluginRouter PluginRouter { get; set; }

        public string Version => "0.1";

        public string Name => "PUBGWeekly";

        public string Description => "AGN PUBG Weekly Helper Plugin - Organise Teams etc";

        public EventRouter EventRouter { get; set; }

        public void Dispose()
        {
        }

        public void ExecutePlugin()
        {
            if (!PubgAPIConfigHandler.Instance.InitJsonConfig()) return;

            GameHandler.Instance.DiscordSocketClient = EventRouter.GetDiscordSocketClient();

            HandlerManager.Instance.RegisterHandler<AdminCommandHandler>();
            HandlerManager.Instance.RegisterHandler<UserCommandHandler>();

            var db = Handler.Instance.GetConnection();
            db.RegisterTable<Configuration.SQL.Configuration>();
            db.RegisterTable<TeamChannels>();
            db.RegisterTable<PubgAccountLink>();

            PluginConfigurator.Instance.LoadConfiguration();
            PubgToDiscordManager.Instance.Load();

            EventRouter.GuildMemberUpdated += EventRouter_GuildMemberUpdated;
            EventRouter.UserVoiceStateUpdated += EventRouter_UserVoiceStateUpdated;
        }

        private Task EventRouter_UserVoiceStateUpdated(SocketUser sktUser, SocketVoiceState voiceStateOld,
            SocketVoiceState voiceStateNew)
        {
            if (voiceStateOld.VoiceChannel != null && voiceStateNew.VoiceChannel != null &&
                voiceStateOld.VoiceChannel.Id == voiceStateNew.VoiceChannel.Id) return Task.CompletedTask;

            if (voiceStateNew.VoiceChannel != null)
                if (GameHandler.Instance.IsLive)
                    if (voiceStateNew.VoiceChannel.Id == (ulong) PluginConfigurator.Instance.Configuration.LobbyId)
                        if (!GameHandler.Instance.GetPlayers().Any(x => x != null && x.DiscordId == sktUser.Id))
                            // The new player is not part of pubg weekly, notify them to join up
                            GameHandler.Instance.SendStatusMessage(
                                $"Hey {sktUser.Mention}, You can join PUBG Weekly by typing `!pubg join` into this channel!");

            return Task.CompletedTask;
        }

        private Task EventRouter_GuildMemberUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
        {
            if (newUser.Status == UserStatus.Offline)
                if (GameHandler.Instance.IsLive)
                    try
                    {
                        GameHandler.Instance.RemovePlayer(newUser.Id);
                        GameHandler.Instance.SendStatusMessage(
                            $"Player {newUser.Username} has been removed from PUBG Weekly as they went offline");
                    }
                    catch (Exception)
                    {
                    }

            return Task.CompletedTask;
        }
    }
}