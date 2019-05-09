using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AGNSharpBot.DiscordHandler;
using AGNSharpBot.PluginHandler;
using CommandHandler;
using Discord;
using GlobalLogger;


namespace AGNSharpBot
{
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
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
                    PluginManager.Instance.Dispose();
                    Logger.Instance.WriteConsole("Shutting down application...");
                    System.Threading.Thread.Sleep(1500);
                    _running = false;
                    return true;
                default:
                    return false;
            }
        }

        public async Task MainAsync()
        {
            Logger.Instance.WriteConsole("AGN Discord Bot Loading...");

            Logger.Instance.WriteConsole("Setting up exit handler capture");
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            try
            {
                Configuration.Discord.Instance.LoadConfiguration();
            }
            catch (Configuration.Exceptions.MissingConfigurationFile)
            {
                Logger.Instance.WriteConsole(
                    "\r\n\r\nYou must provide a config.json file, rename the config.json.example to config.json before loading this application");
                Console.WriteLine("Press <ENTER> to exit");
                Console.ReadKey();
                return;
            }
            catch (Configuration.Exceptions.InvalidConfigurationFile)
            {
                Logger.Instance.WriteConsole(
                    "\r\n\r\nThe provided config.json is invalid, unable to load.");
                Console.WriteLine("Press <ENTER> to exit");
                Console.ReadKey();
                return;
            }

            // Get our discord service definitions
            var serviceHandler = new ServiceDefiner();

            // Start our discord client
            Logger.Instance.WriteConsole("Loading Discord");
            _discordClient.InitDiscordClient(serviceHandler.GetServiceProvider());
            Logger.Instance.SetDiscordClient(Client.Instance.GetDiscordSocket());
            HandlerManager.Instance.RegisterDiscord(Client.Instance.GetDiscordSocket());

            // Load plugins
            PluginManager.Instance.LoadPlugins();

            Logger.Instance.WriteConsole("Connecting to Discord");
            await _discordClient.Connect();         
            await GetUserInputAsync();
        }

        private async Task GetUserInputAsync()
        {
            while (_running)
            {
                await Task.Delay(1000);
                /*var txt = Console.ReadLine();
                switch (txt.ToLower())
                {
                    case "quit":
                    case "exit":
                        if (_discordClient.GetDiscordSocket().ConnectionState == ConnectionState.Connected)
                        {
                            await _discordClient.GetDiscordSocket().LogoutAsync();
                            _discordClient.GetDiscordSocket().Dispose();
                        }

                        return;

                    default:
                        Console.WriteLine("Unknown command, type 'exit' or 'quit' to stop the bot");
                        break;
                }
            }*/
            }
        }
    }
}
