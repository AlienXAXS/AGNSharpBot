using System.Linq;
using GameWatcher.DB.Tables;
using InternalDatabase;

namespace GameWatcher.DB
{
    internal class DatabaseHandler
    {
        private static readonly DatabaseHandler _instance;
        public static DatabaseHandler Instance = _instance ?? (_instance = new DatabaseHandler());

        private readonly Connection _dbConnection;

        public DatabaseHandler()
        {
            _dbConnection = Handler.Instance.NewConnection();
            _dbConnection.RegisterTable<GameMemory>();
        }

        public bool Exists(string name, ulong guildId)
        {
            return _dbConnection.DbConnection.Table<GameMemory>()
                .Any(x => x.Name.Equals(name) && x.GuildId.Equals((long) guildId));
        }

        public void Add(string name, ulong guildId)
        {
            var newRecord = new GameMemory {Name = name, GuildId = (long) guildId};
            _dbConnection.DbConnection.Insert(newRecord);
        }

        public void Remove(string name, ulong guildId)
        {
            var record = _dbConnection.DbConnection.Table<GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long) guildId));

            if (record != null)
                _dbConnection.DbConnection.Delete(record);
        }
    }
}