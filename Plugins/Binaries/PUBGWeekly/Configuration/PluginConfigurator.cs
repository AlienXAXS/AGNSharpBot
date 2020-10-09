using System.Collections.Generic;
using System.Linq;
using InternalDatabase;
using PUBGWeekly.Configuration.SQL;
using SQLite;

namespace PUBGWeekly.Configuration
{
    internal class PluginConfigurator
    {
        public static PluginConfigurator Instance = _instance ?? (_instance = new PluginConfigurator());
        private static readonly PluginConfigurator _instance;

        private SQL.Configuration _configuration;

        private List<TeamChannels> _teamChannels = new List<TeamChannels>();

        private readonly SQLiteConnection dbConnection;


        public PluginConfigurator()
        {
            // Attempt to load initial configuration from db
            dbConnection = Handler.Instance.NewConnection().DbConnection;
        }

        public bool isConfigured { get; set; }

        public SQL.Configuration Configuration
        {
            get => _configuration;

            set
            {
                if (_configuration == null)
                    _configuration = new SQL.Configuration();
                else
                    SaveConfig();

                value = _configuration;
            }
        }

        public void AssignTeamChannel(int team, ulong channel)
        {
            var _foundChannel =
                _teamChannels.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.TeamNumber == team) ??
                new TeamChannels();
            _foundChannel.TeamNumber = team;
            _foundChannel.ChannelId = (long) channel;

            var rowId = dbConnection.Insert(_foundChannel);
            _foundChannel.Id = rowId;

            _teamChannels.Add(_foundChannel);
            SaveConfig();
        }

        public ulong GetTeamChannel(int teamId)
        {
            var _foundTeam = _teamChannels.FirstOrDefault(x => x.TeamNumber == teamId);
            return (ulong) _foundTeam.ChannelId;
        }

        internal void LoadConfiguration()
        {
            var config = dbConnection.Table<SQL.Configuration>().DefaultIfEmpty(null).FirstOrDefault();
            _configuration = config;

            var teamChannels = dbConnection.Table<TeamChannels>().ToList();
            _teamChannels = teamChannels;
        }

        public void SaveConfig()
        {
            if (Configuration == null) Configuration = new SQL.Configuration();

            dbConnection.InsertOrReplace(Configuration);
        }

        internal ulong GetLobbyChannel()
        {
            return (ulong) Configuration.LobbyId;
        }

        internal void AssignStatusChannel(ulong id)
        {
            Configuration.StatusChannel = (long) id;
            SaveConfig();
        }
    }
}