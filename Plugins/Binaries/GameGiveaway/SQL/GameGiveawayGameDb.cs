using SQLite;

namespace Responses.Commands.GameGiveaway.SQL
{
    internal class GameGiveawayGameDb
    {
        // Stores the actual game keys
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        public string Key { get; set; }
        public string Name { get; set; }
        public bool Used { get; set; }
    }
}