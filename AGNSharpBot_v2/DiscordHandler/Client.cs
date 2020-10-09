using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger;

namespace AGNSharpBot.DiscordHandler
{
    internal class Client : IDisposable
    {
        private static readonly Client _instance;
        public static Client Instance = _instance ?? (_instance = new Client());

        private DiscordSocketClient _discordSocket;
        private IServiceProvider _services;

        public async void Dispose()
        {
            if (_discordSocket == null) return;
            await _discordSocket.LogoutAsync();
            await _discordSocket.StopAsync();
            _discordSocket.Dispose();
        }

        public DiscordSocketClient GetDiscordSocket()
        {
            return _discordSocket;
        }

        public void InitDiscordClient(IServiceProvider services)
        {
            _services = services;

            var _config = new DiscordSocketConfig
                {MessageCacheSize = 100, DefaultRetryMode = RetryMode.AlwaysRetry, AlwaysDownloadUsers = true};
            _discordSocket = new DiscordSocketClient(_config);
            _discordSocket.Log += message =>
            {
                if (message.Exception != null)
                    Log4NetHandler.Log($"Discord.NET Message: {message.Message}", Log4NetHandler.LogLevel.ERROR,
                        exception: message.Exception);
                else
                    Log4NetHandler.Log($"Discord.NET Message: {message.Message}", Log4NetHandler.LogLevel.INFO);

                return Task.CompletedTask;
            };
            _discordSocket.Rest.Log += message =>
            {
                Log4NetHandler.Log($"Discord.NET Rest Message: {message.Message}",
                    Log4NetHandler.LogLevel.DEBUG);

                return Task.CompletedTask;
            };
        }

        internal async Task Connect()
        {
            await _discordSocket.LoginAsync(TokenType.Bot, Configuration.Discord.Instance.Token);
            await _discordSocket.StartAsync();
        }
    }
}