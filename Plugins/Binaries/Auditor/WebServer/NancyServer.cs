using System;
using System.Threading;
using Auditor.WebServer.Configuration;
using Discord.WebSocket;
using GlobalLogger;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using Nancy.Session;
using Nancy.TinyIoc;

namespace Auditor.WebServer
{
    internal class NancyServer
    {
        private static readonly NancyServer _instance;
        public static NancyServer Instance = _instance ?? (_instance = new NancyServer());
        private bool _serverRunning;

        private bool _stopRequested;

        public DiscordSocketClient DiscordSocketClient { get; set; }

        public bool GetServerRunning()
        {
            return _serverRunning;
        }

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

            //if ( DiscordSocketClient == null )
            //throw new Exception("DiscordSocketClient not set - unable to start Nancy");

            try
            {
                var newThread = new Thread(() =>
                {
                    var config = ConfigHandler.Instance.Configuration;

                    if (!config.Enabled && !calledFromDiscord)
                    {
                        Log4NetHandler.Log("NancyServer not starting as it's not enabled - skipping init.",
                            Log4NetHandler.LogLevel.INFO);
                        return;
                    }

                    var hostConfigs = new HostConfiguration
                    {
                        UrlReservations = new UrlReservations {CreateAutomatically = true}
                    };

                    using (var nancyHost = new NancyHost(new CustomBootstrapper(), hostConfigs,
                        new Uri($"http://localhost:{config.Port}")))
                    {
                        try
                        {
                            nancyHost.Start();
                            _serverRunning = true;
                            Log4NetHandler.Log($"NancyServer started, listening for connections on port {config.Port}",
                                Log4NetHandler.LogLevel.INFO);

                            // Maybe a better way of doing this, but whatever.
                            while (!_stopRequested) Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Log4NetHandler.Log($"Unable to start NancyServer: {ex.Message}\r\n\r\n{ex.StackTrace}",
                                Log4NetHandler.LogLevel.ERROR, exception: ex);
                        }
                    }

                    _serverRunning = false;
                });

                newThread.Start();
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("NancyServer Start Error - unable to start Nancy", Log4NetHandler.LogLevel.ERROR,
                    exception: ex);
            }
        }

        public void Dispose()
        {
            _stopRequested = true;
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
        protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();

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
            environment.Views(runtimeViewUpdates: true);
        }
    }
}