﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GlobalLogger.AdvancedLogger;
using Newtonsoft.Json;

namespace Auditor.WebServer.Configuration
{
    class StartupConfig
    {
        public bool Enabled { get; set; }

        private IPAddress _ipAddress;
        public IPAddress IpAddress
        {
            get { return _ipAddress ?? new IPAddress(new byte[4] {0, 0, 0, 0}); }
            set => _ipAddress = value;
        }

        private int _port = 0;
        public int Port
        {
            get { return _port == 0 ? 8080 : _port;}
            set => _port = value;
        }

        private string _uri;
        public string URI
        {
            get { return _uri ?? "http://localhost.example.com"; }
            set => _uri = value;
        }
    }


    class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPAddress));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return IPAddress.Parse((string)reader.Value);
        }
    }

    class ConfigHandler
    {
        private static readonly ConfigHandler _instance;
        public static ConfigHandler Instance = _instance ?? (_instance = new ConfigHandler());

        public StartupConfig Configuration = new StartupConfig();

        private const string ConfigFilePath = @"Plugins\Config\Auditor-WebServer.json";

        public ConfigHandler()
        {

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            settings.Formatting = Formatting.Indented;

            try
            {
                // deal with the file not existing yet
                if (!System.IO.File.Exists(ConfigFilePath))
                {
                    System.IO.File.WriteAllText(ConfigFilePath,
                        JsonConvert.SerializeObject(Configuration, Formatting.Indented, settings));
                }
                else
                {
                    try
                    {
                        Configuration =
                            JsonConvert.DeserializeObject<StartupConfig>(System.IO.File.ReadAllText(ConfigFilePath), settings);
                    }
                    catch (Exception ex)
                    {
                        AdvancedLoggerHandler.Instance.GetLogger().Log($"Error while attempting to load config file for Auditor from {ConfigFilePath}");
                        AdvancedLoggerHandler.Instance.GetLogger().Log($"{ex.Message}\r\n\r\n{ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Error while attempting to save default config file for Auditor to {ConfigFilePath}");
                AdvancedLoggerHandler.Instance.GetLogger().Log($"{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }
    }
}