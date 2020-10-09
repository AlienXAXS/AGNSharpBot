using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;

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

            var _config = new DiscordSocketConfig { MessageCacheSize = 100, DefaultRetryMode = RetryMode.AlwaysRetry, AlwaysDownloadUsers = true };
            _discordSocket = new DiscordSocketClient(_config);
            _discordSocket.Log += message =>
            {
                if ( message.Exception != null )
                    GlobalLogger.Log4NetHandler.Log($"Discord.NET Message: {message.Message}", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception:message.Exception);
                else
                    GlobalLogger.Log4NetHandler.Log($"Discord.NET Message: {message.Message}", GlobalLogger.Log4NetHandler.LogLevel.INFO);

                return Task.CompletedTask;
            };
            _discordSocket.Rest.Log += message =>
            {
                GlobalLogger.Log4NetHandler.Log($"Discord.NET Rest Message: {message.Message}",
                    GlobalLogger.Log4NetHandler.LogLevel.DEBUG);

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