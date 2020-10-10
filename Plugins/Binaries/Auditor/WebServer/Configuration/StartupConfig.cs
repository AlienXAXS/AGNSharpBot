using System;
using System.IO;
using System.Net;
using GlobalLogger;
using Newtonsoft.Json;

namespace Auditor.WebServer.Configuration
{
    internal class StartupConfig
    {
        private IPAddress _ipAddress;

        private int _port;

        private string _uri;
        public bool Enabled { get; set; }

        public IPAddress IpAddress
        {
            get { return _ipAddress ?? new IPAddress(new byte[4] {0, 0, 0, 0}); }
            set => _ipAddress = value;
        }

        public int Port
        {
            get => _port == 0 ? 8080 : _port;
            set => _port = value;
        }

        public string URI
        {
            get => _uri ?? "http://localhost.example.com";
            set => _uri = value;
        }
    }

    internal class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPAddress);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            return IPAddress.Parse((string) reader.Value);
        }
    }

    internal class ConfigHandler
    {
        private const string ConfigFilePath = @"Plugins/Config/Auditor-WebServer.json";
        private static readonly ConfigHandler _instance;
        public static ConfigHandler Instance = _instance ?? (_instance = new ConfigHandler());

        public StartupConfig Configuration = new StartupConfig();

        public ConfigHandler()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            settings.Formatting = Formatting.Indented;

            try
            {
                // deal with the file not existing yet
                if (!File.Exists(ConfigFilePath))
                    File.WriteAllText(ConfigFilePath,
                        JsonConvert.SerializeObject(Configuration, Formatting.Indented, settings));
                else
                    try
                    {
                        Configuration =
                            JsonConvert.DeserializeObject<StartupConfig>(File.ReadAllText(ConfigFilePath), settings);
                    }
                    catch (Exception ex)
                    {
                        Log4NetHandler.Log(
                            $"Error while attempting to load config file for Auditor from {ConfigFilePath}",
                            Log4NetHandler.LogLevel.ERROR, exception: ex);
                    }
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log(
                    $"Error while attempting to save default config file for Auditor to {ConfigFilePath}",
                    Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }
    }
}