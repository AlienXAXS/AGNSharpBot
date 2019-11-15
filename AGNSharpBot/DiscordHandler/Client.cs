using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AGNSharpBot.DiscordHandler
{
    internal class Client : IDisposable
    {
        private static Client _instance;
        public static Client Instance = _instance ?? (_instance = new Client());

        private DiscordSocketClient _discordSocket;
        private IServiceProvider _services;

        public DiscordSocketClient GetDiscordSocket() => _discordSocket;

        public void InitDiscordClient(IServiceProvider services)
        {
            _services = services;

            var _config = new DiscordSocketConfig { MessageCacheSize = 100, DefaultRetryMode = RetryMode.AlwaysRetry, AlwaysDownloadUsers = true, LogLevel = LogSeverity.Info };
            _discordSocket = new DiscordSocketClient(_config);
            _discordSocket.Log += message =>
            {
                if (message.Message == null)
                    GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log(message.Exception.Message);
                else
                    GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log(message.Message);
                return Task.CompletedTask;
            };
        }

        internal async Task Connect()
        {
            await _discordSocket.LoginAsync(TokenType.Bot, Configuration.Discord.Instance.Token);
            await _discordSocket.StartAsync();
        }

        public async void Dispose()
        {
            if (_discordSocket == null) return;
            await _discordSocket.LogoutAsync();
            await _discordSocket.StopAsync();
            _discordSocket.Dispose();
        }
    }
}