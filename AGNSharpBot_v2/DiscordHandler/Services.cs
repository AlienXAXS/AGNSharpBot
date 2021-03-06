﻿using System;
using System.Net.Http;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace AGNSharpBot.DiscordHandler
{
    internal class ServiceDefiner
    {
        private readonly IServiceProvider Services;

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