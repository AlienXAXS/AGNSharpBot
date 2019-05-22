using PluginInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger;

namespace AGNSharpBot.PluginHandler
{
    internal class PluginManager : IDisposable
    {
        [ImportMany] // This is a signal to the MEF framework to load all matching exported assemblies.
        private IEnumerable<IPlugin> Plugins { get; set; }

        private static PluginManager _instance;
        public static PluginManager Instance = _instance ?? (_instance = new PluginManager());

        private bool _hasExecutedPlugins = false;

        public void LoadPlugins()
        {
            Logger.Instance.WriteConsole("Loading Plugins from Plugins directory");

            var catalog = new DirectoryCatalog("Plugins");
            using (var container = new CompositionContainer(catalog))
            {
                try
                {
                    container.ComposeParts(this);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    Logger.Instance.WriteConsole("Unable to load plugins...");

                    foreach (var x in ex.LoaderExceptions)
                        Logger.Instance.WriteConsole(x.Message);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteConsole(ex.Message);
                }
            }

            PreExecute();

            DiscordHandler.Client.Instance.GetDiscordSocket().Ready += async () =>
            {
                if (_hasExecutedPlugins) return;
                _hasExecutedPlugins = true;

#if DEBUG
                await Logger.Instance.Log("AGNSharpBot Loading (DEBUG MODE - DEBUGGER ATTACHED TO PROCESS)...", Logger.LoggerType.ConsoleAndDiscord);
#else
                await Logger.Instance.Log("AGNSharpBot Loading...", Logger.LoggerType.ConsoleAndDiscord);
#endif

                var pluginNameList = "";
                Logger.Instance.WriteConsole("Discord Is Ready - Executing Plugins");
                await Logger.Instance.Log($"{Plugins.Count()} plugins are loaded, executing them now.", Logger.LoggerType.DiscordOnly);

                foreach (var plugin in Plugins)
                {
                    pluginNameList += $"{plugin.Name}, ";
                    try
                    {
                        Logger.Instance.WriteConsole($"Pre-Execute Plugin {plugin.Name}");

                        var newThread = new System.Threading.Thread(new ThreadStart(() =>
                        {
                            plugin.ExecutePlugin();
                        }));
                        newThread.IsBackground = true;
                        newThread.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.WriteConsole($"Caught exception on Execute Plugin for {plugin.Name}\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                    }
                }

                await Logger.Instance.Log($"{Plugins.Count()} plugins have been executed -> {pluginNameList}", Logger.LoggerType.DiscordOnly);
            };

            Logger.Instance.WriteConsole("Plugins loaded");
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
                Logger.Instance.WriteConsole($"Plugin '{plugin.Name}' Detected - Init Plugin");
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
