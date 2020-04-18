using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGWeekly.Configuration
{
    class PluginConfigurator
    {

        public static PluginConfigurator Instance = _instance ?? (_instance = new PluginConfigurator());
        private static PluginConfigurator _instance;

        public bool isConfigured { get; set; }

        private SQL.Configuration _configuration;
        public SQL.Configuration Configuration
        {
            get
            {
                return _configuration;
            }

            set {
                if (_configuration == null)
                {
                    _configuration = new SQL.Configuration();
                }
                else
                {
                    SaveConfig();
                }

                value = _configuration;
            }
        }

        private List<SQL.TeamChannels> _teamChannels = new List<SQL.TeamChannels>();

        private SQLite.SQLiteConnection dbConnection;


        public PluginConfigurator()
        {
            // Attempt to load initial configuration from db
            dbConnection = InternalDatabase.Handler.Instance.NewConnection().DbConnection;
        }

        public void AssignTeamChannel(int team, ulong channel)
        {
            var _foundChannel = _teamChannels.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.TeamNumber == team) ?? new SQL.TeamChannels();
            _foundChannel.TeamNumber = team;
            _foundChannel.ChannelId = (long)channel;

            var rowId = dbConnection.Insert(_foundChannel);
            _foundChannel.Id = rowId;

            _teamChannels.Add(_foundChannel);
            SaveConfig();
        }

        public ulong GetTeamChannel(int teamId)
        {
            var _foundTeam = _teamChannels.FirstOrDefault(x => x.TeamNumber == teamId);
            return (ulong)_foundTeam.ChannelId;
        }

        internal void LoadConfiguration()
        {
            var config = dbConnection.Table<SQL.Configuration>().DefaultIfEmpty(null).FirstOrDefault();
            _configuration = config;

            var teamChannels = dbConnection.Table<SQL.TeamChannels>().ToList();
            _teamChannels = teamChannels;
        }

        public void SaveConfig()
        {
            if ( Configuration == null )
            {
                Configuration = new SQL.Configuration();
            }

            dbConnection.InsertOrReplace(Configuration);
        }

        internal ulong GetLobbyChannel()
        {
            return (ulong)Configuration.LobbyId;
        }

        internal void AssignStatusChannel(ulong id)
        {
            Configuration.StatusChannel = (long)id;
            SaveConfig();
        }
    }
}
