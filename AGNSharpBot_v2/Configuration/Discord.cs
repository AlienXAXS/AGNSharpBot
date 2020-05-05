using Newtonsoft.Json;
using System;
using GlobalLogger;

namespace AGNSharpBot.Configuration
{
    internal class Discord
    {
        public static Discord Instance = _instance ?? (_instance = new Discord());
        private static readonly Discord _instance;

        public string Token { get; set; }
        public char CommandPrefix { get; set; }

        public void LoadConfiguration()
        {
            Log4NetHandler.Log("Attempting to load config.json", Log4NetHandler.LogLevel.INFO);
            if (System.IO.File.Exists("config.json"))
            {
                var config = JsonConvert.DeserializeObject<Discord>(System.IO.File.ReadAllText("config.json"));
                if (config.Token.Equals("") || config.CommandPrefix.Equals('\0'))
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
            }
            else
            {
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