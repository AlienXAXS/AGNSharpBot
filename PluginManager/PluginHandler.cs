using Discord.WebSocket;
using Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GlobalLogger;
using ImportManyAttribute = System.ComponentModel.Composition.ImportManyAttribute;

namespace PluginManager
{
    public class PluginHandler
    {
        [ImportMany] // This is a signal to the MEF framework to load all matching exported assemblies.
        private IEnumerable<IPlugin> Plugins { get; set; }

        private static readonly PluginHandler _instance;
        public static PluginHandler Instance = _instance ?? (_instance = new PluginHandler());

        public EventRouter EventRouter = new EventRouter();

        public PluginRouter PluginRouter = new PluginRouter();

        private DiscordSocketClient _discordSocketClient;

        public DiscordSocketClient DiscordSocketClient
        {
            internal get { return _discordSocketClient; }
            set
            {
                _discordSocketClient = value;
                _discordSocketClient.Ready += DiscordSocketClientOnReady;
                EventRouter.SetupEventRouter(value);
            }
        }

        private Task DiscordSocketClientOnReady()
        {
            InitPluginsReady();
            return Task.CompletedTask;
        }

        private bool _hasExecutedPlugins = false;

        public void LoadPlugins()
        {
            Log4NetHandler.Log("Plugin Manager Loading", Log4NetHandler.LogLevel.INFO);

            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<SQL.PluginManager>();

            var catalog = new DirectoryCatalog("Plugins");
            using (var container = new CompositionContainer(catalog))
            {
                try
                {
                    container.ComposeParts(this);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Log4NetHandler.Log("Unable to load one or more plugins", Log4NetHandler.LogLevel.ERROR);

                    foreach (var x in ex.LoaderExceptions)
                        Log4NetHandler.Log($"Plugin failed to load: {x.Message}", Log4NetHandler.LogLevel.ERROR, exception:x);
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log("Fatal error while attempting to compose plugin parts", Log4NetHandler.LogLevel.ERROR, exception:ex);
                }
            }

            Log4NetHandler.Log("Plugin Manager finished loading plugins, will execute them when Discord is ready", Log4NetHandler.LogLevel.INFO);
        }

        public bool ShouldExecutePlugin(ulong guildId, Assembly assembly)
        {
            var pluginName = assembly.ManifestModule.ScopeName;

            var db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();

            var foundEntry = db.DefaultIfEmpty(null).FirstOrDefault(manager =>
                manager != null && manager.PluginName == pluginName && manager.GuildId == Convert.ToInt64(guildId));

            if (foundEntry == null) return false;

            return foundEntry.Enabled;
        }

        public bool ShouldExecutePlugin(string pluginName, ulong guildId)
        {
            var db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();

            var foundEntry = db.DefaultIfEmpty(null).FirstOrDefault(manager => manager != null && manager.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase) && manager.GuildId == Convert.ToInt64(guildId));

            if (foundEntry == null) return false;

            return foundEntry.Enabled;
        }

        private async void InitPluginsReady()
        {
            if (_hasExecutedPlugins) return;
            _hasExecutedPlugins = true;

            Log4NetHandler.Log("AGNSharpBot Plugin System Init Plugins, Discord Status: Ready", Log4NetHandler.LogLevel.INFO);

            var pluginNameList = "";
            Log4NetHandler.Log($"{Plugins.Count()} plugins detected, attempting to load plugins.", Log4NetHandler.LogLevel.INFO);

            foreach (var plugin in Plugins)
            {
                Log4NetHandler.Log($"Plugin {plugin.Name} found, attempting ExecutePlugin procedure.", Log4NetHandler.LogLevel.INFO);

                // Set the event router
                plugin.EventRouter = EventRouter;

                if (plugin is IPluginWithRouter pluginWithRouter)
                {
                    pluginWithRouter.PluginRouter = PluginRouter;
                }

                var newThread = new Thread(() =>
                {
                    try
                    {
#if !DEBUG
                        plugin.ExecutePlugin();
#else
                        if (plugin.Name == "PUBGWeekly")
                        {
                            plugin.ExecutePlugin();
                        }
#endif
                        Log4NetHandler.Log($"Plugin {plugin.Name} ExecutePlugin called successfully", Log4NetHandler.LogLevel.INFO);
                        pluginNameList += $"{plugin.Name}, ";

                    }
                    catch (Exception ex)
                    {
                        Log4NetHandler.Log($"Plugin {plugin.Name} crashed during ExecutePlugin procedure.", Log4NetHandler.LogLevel.ERROR, exception:ex);
                    }
                })
                {
                    IsBackground = true
                };
                newThread.Start();
            }

            Log4NetHandler.Log($"{Plugins.Count()} plugins have been loaded: {pluginNameList}", Log4NetHandler.LogLevel.INFO);
        }

        public IEnumerable<IPlugin> GetPlugins()
        {
            return Plugins;
        }

        public void Dispose()
        {
            foreach (var plugin in Plugins)
            {
                Log4NetHandler.Log($"Disposing plugin {plugin.Name}.", Log4NetHandler.LogLevel.INFO);
                try
                {
                    plugin.Dispose();
                } catch (Exception ex)
                {
                    Log4NetHandler.Log($"Disposing plugin {plugin.Name} failed with an error", Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
            }
        }

        public void SetPluginState(string pluginName, ulong guildId, bool status)
        {
            var foundPlugin = Plugins.DefaultIfEmpty(null).FirstOrDefault(x => x.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
            if (foundPlugin != null)
            {
                var moduleName = foundPlugin.GetType().Assembly.ManifestModule.Name;

                var db = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();
                var pluginEntry = db.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.PluginName.Equals(moduleName, StringComparison.OrdinalIgnoreCase) && x.GuildId.Equals(Convert.ToInt64(guildId)));

                if (pluginEntry == null)
                {
                    pluginEntry = new SQL.PluginManager()
                    {
                        Enabled = status,
                        GuildId = Convert.ToInt64(guildId),
                        PluginName = moduleName
                    };
                    db.Connection.Insert(pluginEntry);
                }
                else
                {
                    pluginEntry.Enabled = status;
                    db.Connection.Update(pluginEntry);
                }
            }
            else
                throw new Exception($"The plugin {pluginName} cannot be found, use !plugins list.");
        }
    }
}