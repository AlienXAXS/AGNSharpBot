using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AGNSharpBot.DiscordHandler;
using CommandHandler;
using GlobalLogger.AdvancedLogger;

namespace AGNSharpBot
{
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        static void Main(string[] args)
        {
            try
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}");

                if ( ex.InnerException != null )
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"Inner Exception Error: {ex.InnerException.Message}\r\n\r\n{ex.InnerException.StackTrace}");
            }
        }
        private readonly Client _discordClient = Client.Instance;

        private delegate bool EventHandler(CtrlType sig);

        static EventHandler _handler;

        private static bool _running = true;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    Client.Instance.Dispose();
                    PluginManager.PluginHandler.Instance.Dispose();
                    GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log("Shutting down application...");
                    System.Threading.Thread.Sleep(1500);
                    _running = false;
                    return true;
                default:
                    return false;
            }
        }

        public async Task MainAsync()
        {
            var logger = GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger()
                .SetRetentionOptions(new RetentionOptions() { Compress = true, Days = 1 })
                .OutputToConsole(true);

            logger.Log("AGNSharpBot is starting up");


            logger.Log("Setting up exit handler capture");
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            try
            {
                Configuration.Discord.Instance.LoadConfiguration();

                // Get our discord service definitions
                var serviceHandler = new ServiceDefiner();

                // Start our discord client
                logger.Log("Loading Discord");
                _discordClient.InitDiscordClient(serviceHandler.GetServiceProvider());
                HandlerManager.Instance.RegisterDiscord(Client.Instance.GetDiscordSocket());

                // Load plugins
                PluginManager.PluginHandler.Instance.DiscordSocketClient = _discordClient.GetDiscordSocket();
                PluginManager.PluginHandler.Instance.LoadPlugins();

                logger.Log("Connecting to Discord");
                await _discordClient.Connect();
                await GetUserInputAsync();
            }
            catch (Configuration.Exceptions.MissingConfigurationFile)
            {
                logger.Log("\r\n\r\nYou must provide a config.json file, rename the config.json.example to config.json before loading this application");
                Console.WriteLine("Press <ENTER> to exit");
                Console.ReadKey();
                return;
            }
            catch (Configuration.Exceptions.InvalidConfigurationFile)
            {
                logger.Log(
                    "\r\n\r\nThe provided config.json is invalid, unable to load.");
                Console.WriteLine("Press <ENTER> to exit");
                Console.ReadKey();
                return;
            }
        }

        private async Task GetUserInputAsync()
        {
            while (_running)
            {
                await Task.Delay(1000);

                try
                {
                    var discordClient = _discordClient.GetDiscordSocket();

                    if ( discordClient == null) continue;

                    var totalUsers = discordClient.Guilds.Sum(guild => guild.MemberCount);

                    Console.Title = $"AGNSharpBot Connected - Guilds:{discordClient.Guilds.Count} - Users:{totalUsers} - Plugins:{PluginManager.PluginHandler.Instance.GetPlugins().Count()}";

                    await discordClient.SetGameAsync($"Serving {totalUsers} users over {discordClient.Guilds.Count} servers.");
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
