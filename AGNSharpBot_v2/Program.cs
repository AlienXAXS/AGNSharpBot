using AGNSharpBot.DiscordHandler;
using CommandHandler;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GlobalLogger;
using log4net;

namespace AGNSharpBot
{
    internal class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private static void Main(string[] args)
        {
            try
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log($"Fatal Error", Log4NetHandler.LogLevel.ERROR, exception:ex);
            }
        }

        private readonly Client _discordClient = Client.Instance;

        private delegate bool EventHandler(CtrlType sig);

        private static EventHandler _handler;
        private static bool _running = true;

        private enum CtrlType
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
                    Log4NetHandler.Log("Shutting down application...", Log4NetHandler.LogLevel.INFO);
                    System.Threading.Thread.Sleep(1500);
                    _running = false;
                    return true;

                default:
                    return false;
            }
        }

        public async Task MainAsync()
        {
            Log4NetHandler.Log("AGNSharpBot is starting up", Log4NetHandler.LogLevel.INFO);

            Log4NetHandler.Log("Setting up exit handler capture", Log4NetHandler.LogLevel.INFO);
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            // Setup our unhandled exception events
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log4NetHandler.Log($"Unhandled Exception in sender: {sender}", Log4NetHandler.LogLevel.ERROR, exception: (Exception)e.ExceptionObject);
            };

            try
            {
                Configuration.Discord.Instance.LoadConfiguration();

                // Get our discord service definitions
                var serviceHandler = new ServiceDefiner();

                // Start our discord client
                Log4NetHandler.Log("Loading Discord", Log4NetHandler.LogLevel.INFO);
                _discordClient.InitDiscordClient(serviceHandler.GetServiceProvider());
                HandlerManager.Instance.RegisterDiscord(Client.Instance.GetDiscordSocket());

                // Load plugins
                PluginManager.PluginHandler.Instance.DiscordSocketClient = _discordClient.GetDiscordSocket();
                if (PluginManager.PluginHandler.Instance.LoadPlugins())
                {
                    Log4NetHandler.Log("Connecting to Discord", Log4NetHandler.LogLevel.INFO);
                    await _discordClient.Connect();
                    await GetUserInputAsync();
                }
                else
                {
                    Log4NetHandler.Log("An error was found during bot startup, check the log for details\r\nHalting operations.", Log4NetHandler.LogLevel.ERROR);
                    Console.ReadLine();
                }
            }
            catch (Configuration.Exceptions.MissingConfigurationFile)
            {
                Log4NetHandler.Log("\r\n\r\nYou must provide a config.json file, rename the config.json.example to config.json before loading this application", Log4NetHandler.LogLevel.ERROR);
                Console.WriteLine("Press <ENTER> to exit");
                Console.ReadKey();
                return;
            }
            catch (Configuration.Exceptions.InvalidConfigurationFile)
            {
                Log4NetHandler.Log("\r\n\r\nThe provided config.json is invalid, unable to load.", Log4NetHandler.LogLevel.ERROR);
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

                    if (discordClient == null) continue;

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