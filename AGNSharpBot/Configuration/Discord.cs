using System;
using Newtonsoft.Json;

namespace AGNSharpBot.Configuration
{
    class Discord
    {
        public static Discord Instance = _instance ?? (_instance = new Discord());
        private static Discord _instance;

        public string Token { get; set; }
        public char CommandPrefix { get; set; }

        public void LoadConfiguration()
        {
            GlobalLogger.Logger.Instance.WriteConsole("Attempting to load config.json");
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

                GlobalLogger.Logger.Instance.WriteConsole("Config parsed and loaded!");
            }
            else
            {
                GlobalLogger.Logger.Instance.WriteConsole("config.json missing, oops!");
                throw new Exceptions.MissingConfigurationFile();
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
}
