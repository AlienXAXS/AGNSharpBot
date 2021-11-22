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

        public bool Exists(string name, ulong guildId, out GameMemory gameRecord)
        {
            var record = _dbConnection.DbConnection.Table<GameMemory>().DefaultIfEmpty(null).FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long) guildId));
            gameRecord = record;

            return record != null;
        }

        public bool Exists(string name, ulong guildId, bool includeAlias = false)
        {
            name = name.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            if (includeAlias)
            {
                return _dbConnection.DbConnection.Table<GameMemory>().Any(x => (x.Name.Equals(name) || name.Equals(x.Alias)) && x.GuildId.Equals((long)guildId));
            }
            else
            {
                var gameMemories = _dbConnection.DbConnection.Table<GameMemory>().ToList().Where(x => x.GuildId.Equals((long)guildId));

                foreach (var memory in gameMemories)
                {
                    if (memory.Name.Equals(name,System.StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;

                //var result = _dbConnection.DbConnection.Table<GameMemory>().Any(x => x.Name.Equals(name) && x.GuildId.Equals((long) guildId));
                //return result;
            }
        }

        public void AddAlias(string name, string alias, ulong guildId)
        {
            name = name.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            alias = alias.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.

            var record = _dbConnection.DbConnection.Table<GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long)guildId));

            if (record == null) throw new GameNotFoundException($"{name} cannot be found, Unable to add alias");

            record.Alias = alias;
            _dbConnection.DbConnection.Update(record);
        }

        public void RemoveAlias(string name, string alias, ulong guildId)
        {
            name = name.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            alias = alias.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            var record = _dbConnection.DbConnection.Table<GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long)guildId));

            if (record == null) throw new GameNotFoundException($"{name} cannot be found, Unable to remove alias");

            record.Alias = null;

            _dbConnection.DbConnection.Update(record);
        }

        public void Add(string name, ulong guildId)
        {
            name = name.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            var newRecord = new GameMemory {Name = name, GuildId = (long) guildId};
            _dbConnection.DbConnection.Insert(newRecord);
        }

        public void Remove(string name, ulong guildId)
        {
            name = name.Replace("\ufe0f", ""); //Remove random unicode from the string that discord puts on there sometimes.
            var record = _dbConnection.DbConnection.Table<GameMemory>().DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name) && x.GuildId.Equals((long) guildId));

            if (record != null)
                _dbConnection.DbConnection.Delete(record);
        }
    }
}