using SQLite;

namespace GameWatcher.DB.Tables
{
    internal class GameMemory
    {
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        [Indexed] public string Name { get; set; }
        public string Alias { get; set; }

        public long GuildId { get; set; }
    }
}