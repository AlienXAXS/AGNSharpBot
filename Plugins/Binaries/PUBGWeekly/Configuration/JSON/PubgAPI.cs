using System;
using System.IO;
using GlobalLogger;
using Newtonsoft.Json;

namespace PUBGWeekly.Configuration.JSON
{
    internal class PubgAPI
    {
        public string ApiKey { get; set; }
    }

    public class PubgAPIConfigHandler
    {
        public static PubgAPIConfigHandler Instance = _instance ?? (_instance = new PubgAPIConfigHandler());
        private static readonly PubgAPIConfigHandler _instance;

        private PubgAPI _pubgApi = new PubgAPI();

        public bool InitJsonConfig()
        {
            var configPath = @".\Plugins\Config\PubgWeeklyApi.json";

            try
            {
                if (File.Exists(configPath))
                {
                    _pubgApi = JsonConvert.DeserializeObject<PubgAPI>(File.ReadAllText(configPath));
                    return true;
                }

                // Generate a blank config
                File.WriteAllText(configPath, JsonConvert.SerializeObject(_pubgApi, Formatting.Indented));
                return false;
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("[PubgWeekly-InitJsonConfig] Error attempting to load json api config",
                    Log4NetHandler.LogLevel.ERROR, exception: ex);
                return false;
            }
        }

        public string GetApiKey()
        {
            return _pubgApi.ApiKey;
        }
    }
}