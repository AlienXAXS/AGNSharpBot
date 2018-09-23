using System;
using System.Net.Http;
using AGNSharpBot.DiscordHandler.Services;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace AGNSharpBot.DiscordHandler
{
    class ServiceDefiner
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
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<PictureService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}
