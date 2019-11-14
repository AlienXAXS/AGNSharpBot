using System.Linq;

namespace JoinQuitMessages.Configuration
{
    class ConfigurationHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ConfigurationHandler _instance;
        public static ConfigurationHandler Instance = _instance ?? (_instance = new ConfigurationHandler());

        public void AssignChannel(ulong GuildId, ulong ChannelId)
        {
            var guildConfiguration = GetConfiguration(GuildId) ?? new SQLTables.Configuration();

            guildConfiguration.GuildId = (long)GuildId;
            guildConfiguration.ChannelId = (long)ChannelId;

            // Grab our database session
            var database = InternalDatabase.Handler.Instance.GetConnection();

            // Insert the new row into the database.
            database.DbConnection.Table<SQLTables.Configuration>().Connection.Insert(guildConfiguration);
        }

        public SQLTables.Configuration GetConfiguration(ulong GuildId)
        {
            var database = InternalDatabase.Handler.Instance.GetConnection();
            var config = database?.DbConnection.Table<SQLTables.Configuration>().DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.GuildId.Equals((long) GuildId));
            return config;
        }
    }
}
