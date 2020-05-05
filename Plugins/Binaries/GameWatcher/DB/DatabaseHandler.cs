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

        public bool Exists(string name)
        {
            return _dbConnection.DbConnection.Table<Tables.GameMemory>().Any(x => x.Name.Equals(name));
        }

        public void Add(string name)
        {
            var newRecord = new Tables.GameMemory() { Name = name };
            _dbConnection.DbConnection.Insert(newRecord);
        }

        public void Remove(string name)
        {
            var record = _dbConnection.DbConnection.Table<Tables.GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name));

            if (record != null)
                _dbConnection.DbConnection.Delete(record);
        }
    }
}