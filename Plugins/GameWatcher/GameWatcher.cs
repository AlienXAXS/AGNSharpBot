﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using GameWatcher.DB;
using GlobalLogger.AdvancedLogger;
using Interface;
using PluginManager;

namespace GameWatcher
{
    [Export(typeof(IPlugin))]
    public class GameWatcher : IPlugin
    {
        public EventRouter EventRouter { get; set; }
        public string Name => "GameWatcher";

        public string Description =>
            "Watches the guild for game activity, and automatically assigns roles to people playing games.";

        private DiscordSocketClient _discordClient;

        public void ExecutePlugin()
        {
            try
            {
                AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true).SetRetentionOptions(new RetentionOptions() {Compress = true});
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER] Checking Discord...");
                AdvancedLoggerHandler.Instance.GetLogger().Log($"[GAMEWATCHER]  >  Complete");

                _discordClient = EventRouter.GetDiscordSocketClient();
                GameHandler.Instance.DiscordSocketClient = _discordClient;

                CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.Control>();
                EventRouter.GuildMemberUpdated += DiscordClientOnGuildMemberUpdatedEvent;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private async Task DiscordClientOnGuildMemberUpdatedEvent(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            await DiscordClientOnGuildMemberUpdated(arg1, arg2);
        }

        private async Task DiscordClientOnGuildMemberUpdated(SocketGuildUser oldGuildUser, SocketGuildUser newGuildUser, bool firstRun = false)
        {
            await GameHandler.Instance.GameScan(oldGuildUser, newGuildUser);
        }

        public void Dispose()
        {
            GameHandler.Instance.Dispose();
        }
    }
}
