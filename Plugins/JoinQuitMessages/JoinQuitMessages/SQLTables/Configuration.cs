using SQLite;

namespace JoinQuitMessages.SQLTables
{
    internal class Configuration
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long GuildId { get; set; }
        public long ChannelId { get; set; }
    }
}