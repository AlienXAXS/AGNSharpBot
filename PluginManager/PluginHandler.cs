using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger;
using Interface;
using InternalDatabase;

namespace PluginManager
{
    public class PluginHandler
    {
        private static readonly PluginHandler _instance;
        public static PluginHandler Instance = _instance ?? (_instance = new PluginHandler());

        private DiscordSocketClient _discordSocketClient;

        private bool _hasExecutedPlugins;

        public EventRouter EventRouter = new EventRouter();

        public PluginRouter PluginRouter = new PluginRouter();

        [ImportMany] // This is a signal to the MEF framework to load all matching exported assemblies.
        private IEnumerable<IPlugin> Plugins { get; set; }

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

        /// <summary>
        /// Scans plugins from the Plugins directory, and reports on any plugin DLL's that are not correctly exporting the interface.
        /// </summary>
        /// <returns></returns>
        public bool LoadPlugins()
        {
            Log4NetHandler.Log("Plugin Manager Loading", Log4NetHandler.LogLevel.INFO);

            Handler.Instance.NewConnection().RegisterTable<SQL.PluginManager>();

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
                        Log4NetHandler.Log($"Plugin failed to load: {x.Message}", Log4NetHandler.LogLevel.ERROR,
                            exception: x);
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log("Fatal error while attempting to compose plugin parts",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }

                // Here we check to see if the amount of DLL's in the plugins directory matches the amount of composed DLL's (ones that match the interface, and export it)
                // This is a new method, which easily alerts what DLL's are missing interfaces or are slightly wrong in terms of their implementation or exports.
                if (container.Catalog.Parts.Count() != catalog.LoadedFiles.Count)
                {
                    Log4NetHandler.Log(
                        $"Fatal Error: We have {catalog.LoadedFiles.Count} plugin binaries, but only {Plugins.Count()} plugins could locate their injection point.\r\n---\r\nInvalid plugins are listed below:",
                        Log4NetHandler.LogLevel.ERROR);

                    foreach (var item in catalog.LoadedFiles)
                    {
                        var dllName = Path.GetFileNameWithoutExtension(item).ToLower();
                        var loaded =
                            container.Catalog.Parts.Any(x => x != null && x.ToString().ToLower().Contains(dllName));
                        if (!loaded)
                            Log4NetHandler.Log($"{item} Loaded: {loaded}", Log4NetHandler.LogLevel.ERROR);
                    }

                    return false;
                }
            }

            Log4NetHandler.Log("Plugin Manager finished loading plugins, will execute them when Discord is ready",
                Log4NetHandler.LogLevel.INFO);
            return true;
        }

        public bool ShouldExecutePlugin(ulong guildId, Assembly assembly)
        {
            var pluginName = assembly.ManifestModule.ScopeName;

            var db = Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();

            var foundEntry = db.DefaultIfEmpty(null).FirstOrDefault(manager =>
                manager != null && manager.PluginName == pluginName && manager.GuildId == Convert.ToInt64(guildId));

            if (foundEntry == null) return false;

            return foundEntry.Enabled;
        }

        public bool ShouldExecutePlugin(string pluginName, ulong guildId)
        {
            var db = Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();

            var foundEntry = db.DefaultIfEmpty(null).FirstOrDefault(manager =>
                manager != null && manager.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase) &&
                manager.GuildId == Convert.ToInt64(guildId));

            if (foundEntry == null) return false;

            return foundEntry.Enabled;
        }

        private async void InitPluginsReady()
        {
            if (_hasExecutedPlugins) return;
            _hasExecutedPlugins = true;

            Log4NetHandler.Log("AGNSharpBot Plugin System Init Plugins, Discord Status: Ready",
                Log4NetHandler.LogLevel.INFO);

            var pluginNameList = "";
            Log4NetHandler.Log($"{Plugins.Count()} plugins detected, attempting to load plugins.",
                Log4NetHandler.LogLevel.INFO);

            foreach (var plugin in Plugins)
            {
                Log4NetHandler.Log($"Plugin {plugin.Name} found, attempting ExecutePlugin procedure.",
                    Log4NetHandler.LogLevel.INFO);

                // Set the event router
                plugin.EventRouter = EventRouter;

                if (plugin is IPluginWithRouter pluginWithRouter) pluginWithRouter.PluginRouter = PluginRouter;

                try
                {
                    var newThread = new Thread(() => { plugin.ExecutePlugin(); })
                    {
                        IsBackground = true
                    };
                    newThread.Start();
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log($"Plugin {plugin.Name} crashed during ExecutePlugin procedure.",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }

                Log4NetHandler.Log($"Plugin {plugin.Name} ExecutePlugin called successfully",
                    Log4NetHandler.LogLevel.INFO);
                pluginNameList += $"{plugin.Name}, ";
            }

            Log4NetHandler.Log($"{Plugins.Count()} plugins have been loaded: {pluginNameList}",
                Log4NetHandler.LogLevel.INFO);
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
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log($"Disposing plugin {plugin.Name} failed with an error",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
            }
        }

        public void SetPluginState(string pluginName, ulong guildId, bool status)
        {
            var foundPlugin = Plugins.DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
            if (foundPlugin != null)
            {
                var moduleName = foundPlugin.GetType().Assembly.ManifestModule.Name;

                var db = Handler.Instance.GetConnection().DbConnection.Table<SQL.PluginManager>();
                var pluginEntry = db.DefaultIfEmpty(null).FirstOrDefault(x =>
                    x != null && x.PluginName.Equals(moduleName, StringComparison.OrdinalIgnoreCase) &&
                    x.GuildId.Equals(Convert.ToInt64(guildId)));

                if (pluginEntry == null)
                {
                    pluginEntry = new SQL.PluginManager
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
            {
                throw new Exception($"The plugin {pluginName} cannot be found, use !plugins list.");
            }
        }
    }
}