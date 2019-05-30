using System;
using GlobalLogger.AdvancedLogger;
using Newtonsoft.Json;

namespace AGNSharpBot.Configuration
{
    class Discord
    {
        public static Discord Instance = _instance ?? (_instance = new Discord());
        private static readonly Discord _instance;

        public string Token { get; set; }
        public char CommandPrefix { get; set; }

        public void LoadConfiguration()
        {
            AdvancedLoggerHandler.Instance.GetLogger().Log("Attempting to load config.json");
            if (System.IO.File.Exists("config.json"))
            {
                var config = JsonConvert.DeserializeObject<Discord>(System.IO.File.ReadAllText("config.json"));
                if ( config.Token.Equals("") || config.CommandPrefix.Equals('\0') )
                    throw new Exceptions.InvalidConfigurationFile();

                try
                {
                    Token = config.Token;
                    CommandPrefix = config.CommandPrefix;
                }
                catch (Exception ex)
                {
                    throw new Exceptions.InvalidConfigurationFile(ex.Message);
                }

                AdvancedLoggerHandler.Instance.GetLogger().Log("Config parsed and loaded!");
            }
            else
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log("config.json missing, oops!");
                throw new Exceptions.MissingConfigurationFile();
            }
        }
    }
    public class Exceptions
    {
        public class MissingConfigurationFile : Exception
        {
        }

        public class InvalidConfigurationFile : Exception
        {
            public InvalidConfigurationFile(string message) : base(message)
            {
            }

            public InvalidConfigurationFile()
            {
            }
        }
    }
}
