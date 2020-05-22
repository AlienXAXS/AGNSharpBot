using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PUBGWeekly.Configuration.SQL;

namespace PUBGWeekly.Configuration
{
    class PubgToDiscordManager
    {
        public static PubgToDiscordManager Instance = _instance ?? (_instance = new PubgToDiscordManager());
        private static readonly PubgToDiscordManager _instance;

        public List<SQL.PubgAccountLink> PubgAccountLinks = new List<PubgAccountLink>();

        private readonly SQLite.SQLiteConnection _dbConnection;

        public PubgToDiscordManager()
        {
            _dbConnection = InternalDatabase.Handler.Instance.NewConnection().DbConnection;
        }

        public void Load()
        {
            var pubgToDiscordLinks = _dbConnection.Table<SQL.PubgAccountLink>().ToList();
            PubgAccountLinks = pubgToDiscordLinks;
        }

        public void Add(string pubgId, ulong discordId)
        {
            var newRecord = new PubgAccountLink() {DiscordId = (long) discordId, PubgAccountId = pubgId};
            PubgAccountLinks.Add(newRecord);
            _dbConnection.Insert(newRecord);
        }
    }
}
