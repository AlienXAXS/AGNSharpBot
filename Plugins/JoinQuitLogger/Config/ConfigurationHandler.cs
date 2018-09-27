using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GlobalLogger;
using Newtonsoft.Json;

namespace JoinQuitLogger.Config
{
    class ConfigurationHandler
    {

        public class DateTimeValue
        {
            public DateTime Value { get; set; }

            public DateTimeValue(DateTime value)
            {
                Value = value;
            }
        }

        public class UserData
        {
            public ulong UserId;
            public DateTimeValue JoinedDate;
            public DateTimeValue LeftDate;
            public int TimesJoined;

            public UserData(ulong userId, DateTimeValue joinedDate, DateTimeValue leftDate, int timesJoined)
            {
                UserId = userId;
                JoinedDate = joinedDate;
                LeftDate = leftDate;
                TimesJoined = timesJoined;
            }
        }

        public class DiscordGuildIdentifier
        {
            public ulong GuildId = 0;
            public ulong ChannelId = 0;
        }

        public class ConfigRoot
        {
            // Database for our user data
            public List<UserData> UserData = new List<UserData>();
            public DiscordGuildIdentifier JoinLeaveMessageOutput = new DiscordGuildIdentifier();
        }

        private static ConfigurationHandler _instance;
        public static ConfigurationHandler Instance = _instance ?? (_instance = new ConfigurationHandler());

        public readonly ConfigRoot ConfigurationRoot = new ConfigRoot();

        private const string ConfigurationPath = "Plugins\\Config\\JoinQuitLogger.json";

        public void Init() { }
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public void UserLeft(SocketGuildUser sktGuildUser)
        {

        }

        public async Task<UserData> UserJoined(SocketGuildUser sktGuildUser)
        {
            try
            {
                var finder = GetUserFromDiscordId(sktGuildUser.Id);

                if (finder != null)
                {
                    finder.TimesJoined++;
                    Save();

                    return finder;
                }
                else
                {
                    ConfigurationRoot.UserData.Add(new UserData(sktGuildUser.Id, new DateTimeValue(DateTime.Now), null,
                        1));
                    Save();

                    return ConfigurationRoot.UserData[ConfigurationRoot.UserData.Count - 1];
                }
            }
            catch (Exception ex)
            {
                await Logger.Instance.Log($"{ex.Message}\r\n\r\n{ex.StackTrace}", Logger.LoggerType.ConsoleOnly);
                return null;
            }
        }

        public UserData GetUserFromDiscordId(ulong userId)
        {
            return ConfigurationRoot.UserData.Where(x => x.UserId == userId).DefaultIfEmpty(null)
                .FirstOrDefault();
        }

        public void Save()
        {
            // Only allow one thread to access the file at once.
            SemaphoreSlim.WaitAsync();
            System.IO.File.WriteAllText(ConfigurationPath, JsonConvert.SerializeObject(ConfigurationRoot, Formatting.Indented));
            SemaphoreSlim.Release();
        }

        public ConfigurationHandler()
        {
            try
            {
                if (System.IO.File.Exists(ConfigurationPath))
                {
                    ConfigurationRoot =
                        JsonConvert.DeserializeObject<ConfigRoot>(System.IO.File.ReadAllText(ConfigurationPath));
                }
                else
                {
                    // Create a default config file instead
                    System.IO.File.WriteAllText(ConfigurationPath, JsonConvert.SerializeObject(ConfigurationRoot, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                var x = Task.Run(async () => {
                    await Logger.Instance.Log(
                        $"Unable to deserialise configuration for JoinQuitLogger\r\n{ex.Message}\r\n\r\n{ex.StackTrace}",
                        Logger.LoggerType.ConsoleOnly);
                });
                x.Wait();
            }
        }
    }
}
