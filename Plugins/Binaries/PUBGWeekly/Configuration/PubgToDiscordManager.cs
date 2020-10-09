using System.Collections.Generic;
using InternalDatabase;
using PUBGWeekly.Configuration.SQL;
using SQLite;

namespace PUBGWeekly.Configuration
{
    internal class PubgToDiscordManager
    {
        public static PubgToDiscordManager Instance = _instance ?? (_instance = new PubgToDiscordManager());
        private static readonly PubgToDiscordManager _instance;

        private readonly SQLiteConnection _dbConnection;

        public List<PubgAccountLink> PubgAccountLinks = new List<PubgAccountLink>();

        public PubgToDiscordManager()
        {
            _dbConnection = Handler.Instance.NewConnection().DbConnection;
        }

        public void Load()
        {
            var pubgToDiscordLinks = _dbConnection.Table<PubgAccountLink>().ToList();
            PubgAccountLinks = pubgToDiscordLinks;
        }

        public void Add(string pubgId, ulong discordId)
        {
            var newRecord = new PubgAccountLink {DiscordId = (long) discordId, PubgAccountId = pubgId};
            PubgAccountLinks.Add(newRecord);
            _dbConnection.Insert(newRecord);
        }
    }
}