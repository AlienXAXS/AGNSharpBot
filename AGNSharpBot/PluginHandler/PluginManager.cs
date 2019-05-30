using PluginInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using Logger = GlobalLogger.Logger;

namespace AGNSharpBot.PluginHandler
{
    internal class PluginManager : IDisposable
    {
        [ImportMany] // This is a signal to the MEF framework to load all matching exported assemblies.
        private IEnumerable<IPlugin> Plugins { get; set; }

        private static PluginManager _instance;
        public static PluginManager Instance = _instance ?? (_instance = new PluginManager());

        private bool _hasExecutedPlugins = false;

        public PluginManager()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() {Compress = true});
        }

        public void LoadPlugins()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("Loading Plugins from Plugins directory");

            var catalog = new DirectoryCatalog("Plugins");
            using (var container = new CompositionContainer(catalog))
            {
                try
                {
                    container.ComposeParts(this);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    AdvancedLoggerHandler.Instance.GetLogger().Log("Unable to load plugins...");

                    foreach (var x in ex.LoaderExceptions)
                        AdvancedLoggerHandler.Instance.GetLogger().Log(x.Message);
                }
                catch (Exception ex)
                {
                    AdvancedLoggerHandler.Instance.GetLogger().Log(ex.Message);
                }
            }

            PreExecute();

            DiscordHandler.Client.Instance.GetDiscordSocket().Ready += async () =>
            {
                if (_hasExecutedPlugins) return;
                _hasExecutedPlugins = true;

#if DEBUG
                AdvancedLoggerHandler.Instance.GetLogger().Log("AGNSharpBot Loading (DEBUG MODE - DEBUGGER ATTACHED TO PROCESS)...");
#else
                AdvancedLoggerHandler.Instance.GetLogger().Log("AGNSharpBot Loading...");
#endif

                var pluginNameList = "";
                AdvancedLoggerHandler.Instance.GetLogger().Log("Discord Is Ready - Executing Plugins");
                await Logger.Instance.Log($"{Plugins.Count()} plugins are loaded, executing them now.", Logger.LoggerType.DiscordOnly);

                foreach (var plugin in Plugins)
                {
                    pluginNameList += $"{plugin.Name}, ";
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"Pre-Execute Plugin {plugin.Name}");

                    var newThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            plugin.ExecutePlugin();
                        }
                        catch (Exception ex)
                        {
                            AdvancedLoggerHandler.Instance.GetLogger().Log($"Caught exception on Execute Plugin for {plugin.Name}\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                        }
                    })
                    {
                        IsBackground = true
                    };
                    newThread.Start();
                }

                await Logger.Instance.Log($"{Plugins.Count()} plugins have been executed -> {pluginNameList}", Logger.LoggerType.DiscordOnly);
            };

            AdvancedLoggerHandler.Instance.GetLogger().Log("Plugins loaded");
        }
        
        public IEnumerable<IPlugin> GetPlugins()
        {
            return Plugins;
        }

        private void PreExecute()
        {
            if (Plugins == null) return;

            foreach (var plugin in Plugins)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Plugin '{plugin.Name}' Detected - Init Plugin");
                plugin.DiscordClient = DiscordHandler.Client.Instance.GetDiscordSocket();
            }
        }

        public void Dispose()
        {
            foreach (var plugin in Plugins)
                plugin.Dispose();
        }
    }
}
