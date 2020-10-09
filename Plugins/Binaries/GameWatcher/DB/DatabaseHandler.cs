using System.Linq;

namespace GameWatcher.DB
{
    internal class DatabaseHandler
    {
        private static DatabaseHandler _instance;
        public static DatabaseHandler Instance = _instance ?? (_instance = new DatabaseHandler());

        private readonly InternalDatabase.Connection _dbConnection;

        public DatabaseHandler()
        {
            _dbConnection = InternalDatabase.Handler.Instance.NewConnection();
            _dbConnection.RegisterTable<Tables.GameMemory>();
        }

        public bool Exists(string name, ulong guildId)
        {
            return _dbConnection.DbConnection.Table<Tables.GameMemory>().Any(x => x.Name.Equals(name) && x.GuildId.Equals((long)guildId));
        }

        public void Add(string name, ulong guildId)
        {
            var newRecord = new Tables.GameMemory() { Name = name, GuildId = (long)guildId};
            _dbConnection.DbConnection.Insert(newRecord);
        }

        public void Remove(string name, ulong guildId)
        {
            var record = _dbConnection.DbConnection.Table<Tables.GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long)guildId));

            if (record != null)
                _dbConnection.DbConnection.Delete(record);
        }
    }
}