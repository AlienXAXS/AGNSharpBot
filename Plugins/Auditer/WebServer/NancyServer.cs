using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using Nancy.ViewEngines.SuperSimpleViewEngine;

namespace Auditor.WebServer
{
    class NancyServer
    {

        private bool stopRequested = false;

        public void Stop()
        {
            stopRequested = true;
        }

        public void Start()
        {
            var newThread = new Thread(() =>
            {
                using (var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri("http://localhost:8080/"), new CustomBootstrapper()))
                {
                    nancyHost.Start();

                    while (!stopRequested)
                    {
                        Thread.Sleep(1000);
                    }
                }
            });

            newThread.Start();
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

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            environment.Views(runtimeViewUpdates:true);
        }

        protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();
    }
}
