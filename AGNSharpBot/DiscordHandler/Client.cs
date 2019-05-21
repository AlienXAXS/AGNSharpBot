using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace AGNSharpBot.DiscordHandler
{
    class Client : IDisposable
    {
        private static Client _instance;
        public static Client Instance = _instance ?? (_instance = new Client());

        private DiscordSocketClient _discordSocket;
        private IServiceProvider _services;

        public DiscordSocketClient GetDiscordSocket() => _discordSocket;

        public void InitDiscordClient(IServiceProvider services)
        {
            _services = services;

            var _config = new DiscordSocketConfig { MessageCacheSize = 100 };
            //_discordSocket = services.GetRequiredService<DiscordSocketClient>();
            _discordSocket = new DiscordSocketClient(_config);

            _discordSocket.Log += message =>
            {
                GlobalLogger.Logger.Instance.WriteConsole(message.Message);
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