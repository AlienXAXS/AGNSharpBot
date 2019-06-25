using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Extensions;
using Nancy.Hosting.Self;
using Nancy.Session;
using Nancy.TinyIoc;
using Nancy.ViewEngines.SuperSimpleViewEngine;
using Newtonsoft.Json;

namespace Auditor.WebServer
{
    class NancyServer
    {
        private static readonly NancyServer _instance;
        public static NancyServer Instance = _instance ?? (_instance = new NancyServer());

        public DiscordSocketClient DiscordSocketClient { get; set; }

        private bool _stopRequested = false;
        private bool _serverRunning = false;

        public void Stop()
        {
            if (!_serverRunning)
                throw new Exception("NancyServer is not running.");

            _stopRequested = true;
        }

        public void Start(bool calledFromDiscord = false)
        {
            if (_serverRunning)
                throw new Exception("NancyServer is already running");

            if ( DiscordSocketClient == null )
                throw new Exception("DiscordSocketClient not set - unable to start Nancy");

            try
            {
                var newThread = new Thread(() =>
                {
                    var config = Configuration.ConfigHandler.Instance.Configuration;

                    if (!config.Enabled && !calledFromDiscord)
                    {
                        AdvancedLoggerHandler.Instance.GetLogger().Log("NancyServer not starting as it's not enabled - skipping init.");
                        return;
                    }

                    using (var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri($"http://localhost:{config.Port}"),
                        new CustomBootstrapper()))
                    {
                        try
                        {
                            nancyHost.Start();
                            _serverRunning = true;
                            AdvancedLoggerHandler.Instance.GetLogger().Log($"NancyServer started, listening for connections on port {config.Port}");

                            // Maybe a better way of doing this, but whatever.
                            while (!_stopRequested)
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to start NancyServer: {ex.Message}\r\n\r\n{ex.StackTrace}");
                        }
                    }

                    _serverRunning = false;
                });

                newThread.Start();
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"NancyServer Start Error: {ex.Message}");
            }
        }
    }


    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return "html_docs";
        }
    }

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);
            conventions.StaticContentsConventions.AddDirectory("/inc");
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            CookieBasedSessions.Enable(pipelines);
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            environment.Views(runtimeViewUpdates:true);
        }

        protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();
    }
}
