using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace AGNSharpBot.DiscordHandler
{
    internal class ServiceDefiner
    {
        private IServiceProvider Services;

        public ServiceDefiner()
        {
            Services = ConfigureServices();
        }

        public IServiceProvider GetServiceProvider()
        {
            return Services;
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}