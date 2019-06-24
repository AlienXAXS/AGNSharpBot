using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Auditor.WebServer
{
    class Configuration
    {
        private static readonly Configuration _instance;
        public static Configuration Instance = _instance ?? (_instance = new Configuration());

        private IPAddress _ipAddress;
        public IPAddress IpAddress => _ipAddress;

        private int _port;
        public int Port => _port;

        private bool _autoStart;
        public bool AutoStart => _autoStart;

        private string _uri;
        public string Uri => _uri;

        private ulong _guildId;
        public ulong GuildId => _guildId;

        public void UpdateConfiguration(IPAddress ipAddress, int port, bool autoStart, string uri, ulong guildId)
        {
            _ipAddress = ipAddress;
            _port = port;
            _autoStart = autoStart;
            _uri = uri;
            _guildId = guildId;

            var wsSettings = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql.WebServerSettings>();

            // Drop all data from the table
            wsSettings.Connection.DeleteAll(wsSettings.Table);

            wsSettings.Connection.Insert(new AuditorSql.WebServerSettings()
                {Enabled = autoStart, IpAddress = ipAddress.ToString(), Port = port, URI = uri, GuildId = (long)_guildId});

        }


    }
}
