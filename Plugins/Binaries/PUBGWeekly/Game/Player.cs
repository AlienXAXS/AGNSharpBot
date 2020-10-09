namespace PUBGWeekly.Game
{
    internal class Player
    {
        public ulong DiscordId;
        public string Name;
        public int Team;

        public Player(string name, ulong discordId)
        {
            Name = name;
            DiscordId = discordId;
        }
    }
}