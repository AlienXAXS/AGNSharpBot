using Newtonsoft.Json;
using System;

namespace GlobalLogger.Configuration
{
    internal class uLongValueContainer
    {
        public ulong Value { get; set; }
    }

    internal class LoggerDiscordConfiguration
    {
        public string _comment_DiscordChannelId = "The discord channel ID that you wish to use for logger messages";
        public uLongValueContainer DiscordChannelId { get; set; } = new uLongValueContainer();

        public string _comment_DiscordGuildId = "This discord guild ID where the channel ID above resides";
        public uLongValueContainer DiscordGuildId { get; set; } = new uLongValueContainer();
    }

    internal class Discord
    {
        private const string ConfigurationPath = "config_logger.json";
        private static Discord _instance;
        public static Discord Instance = _instance ?? (_instance = new Discord());

        private LoggerDiscordConfiguration _discordConfiguration;

        public Discord()
        {
            AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true).SetRetentionOptions(new AdvancedLogger.RetentionOptions() { Days = 1, Compress = true });
        }

        public ulong GetDiscordLoggerChannelId()
        {
            return _discordConfiguration?.DiscordChannelId?.Value ?? 0;
        }

        public ulong GetDiscordGuildId()
        {
            return _discordConfiguration?.DiscordGuildId?.Value ?? 0;
        }

        public void LoadConfiguration()
        {
            if (System.IO.File.Exists(ConfigurationPath))
            {
                try
                {
                    _discordConfiguration =
                        JsonConvert.DeserializeObject<LoggerDiscordConfiguration>(
                            System.IO.File.ReadAllText(ConfigurationPath));
                }
                catch (JsonException jsonException)
                {
                    AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to deserialize default configuration for logger discord config\r\n{jsonException.Message}");
                }
                catch (Exception ex)
                {
                    AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to read default configuration for logger discord config to file ({ConfigurationPath})\r\n{ex.Message}");
                }
            }
            else
            {
                try
                {
                    System.IO.File.WriteAllText(ConfigurationPath,
                        JsonConvert.SerializeObject(new LoggerDiscordConfiguration(), Formatting.Indented));
                }
                catch (JsonException jsonException)
                {
                    AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to serialize default configuration for logger discord config\r\n{jsonException.Message}");
                }
                catch (Exception ex)
                {
                    AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to write default configuration for logger discord config to file\r\n{ex.Message}");
                }
            }
        }
    }
}