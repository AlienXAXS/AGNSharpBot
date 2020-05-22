using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlobalLogger;
using Newtonsoft.Json;

namespace PUBGWeekly.Configuration.JSON
{
    class PubgAPI
    {
        public string ApiKey { get; set; }
    }

    public class PubgAPIConfigHandler
    {
        public static PubgAPIConfigHandler Instance = _instance ?? (_instance = new PubgAPIConfigHandler());
        private static readonly PubgAPIConfigHandler _instance;

        private Configuration.JSON.PubgAPI _pubgApi = new PubgAPI();

        public bool InitJsonConfig()
        {
            string configPath = @".\Plugins\Config\PubgWeeklyApi.json";

            try
            {
                if (System.IO.File.Exists(configPath))
                {
                    _pubgApi = JsonConvert.DeserializeObject<Configuration.JSON.PubgAPI>(System.IO.File.ReadAllText(configPath));
                    return true;
                }
                else
                {
                    // Generate a blank config
                    System.IO.File.WriteAllText(configPath, JsonConvert.SerializeObject(_pubgApi, Formatting.Indented));
                    return false;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log("[PubgWeekly-InitJsonConfig] Error attempting to load json api config", Log4NetHandler.LogLevel.ERROR, exception: ex);
                return false;
            }
        }

        public string GetApiKey() => _pubgApi.ApiKey;
    }
}
