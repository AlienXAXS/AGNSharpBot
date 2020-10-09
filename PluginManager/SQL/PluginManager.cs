using SQLite;

namespace PluginManager.SQL
{
    internal class PluginManager
    {
        [PrimaryKey] [AutoIncrement] public long id { get; set; }

        public string PluginName { get; set; }
        public bool Enabled { get; set; }
        public long GuildId { get; set; }
    }
}