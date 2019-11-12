using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using Interface;

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
            internal get { return _discordSocketClient;}
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

        public PluginHandler()
        {
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true).SetRetentionOptions(new RetentionOptions() { Compress = true });
            InternalDatabase.Handler.Instance.NewConnection().RegisterTable<SQL.PluginManager>();
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

            AdvancedLoggerHandler.Instance.GetLogger().Log("Plugins loaded");
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

            AdvancedLoggerHandler.Instance.GetLogger().Log("AGNSharpBot Loading...");

            var pluginNameList = "";
            AdvancedLoggerHandler.Instance.GetLogger().Log("Discord Is Ready - Executing Plugins");

            var logger = GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);

            logger.Log($"{Plugins.Count()} plugins are loaded, executing them now.");

            foreach (var plugin in Plugins)
            {
                pluginNameList += $"{plugin.Name}, ";
                logger.Log($"Pre-Execute Plugin {plugin.Name}");

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
                        plugin.ExecutePlugin();
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Caught exception on Execute Plugin for {plugin.Name}\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                    }
                })
                {
                    IsBackground = true
                };
                newThread.Start();
            }

            logger.Log($"{Plugins.Count()} plugins have been executed -> {pluginNameList}");
        }

        public IEnumerable<IPlugin> GetPlugins()
        {
            return Plugins;
        }
        
        public void Dispose()
        {
            foreach (var plugin in Plugins)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Shutting down plugin {plugin.Name}.");
                plugin.Dispose();
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
