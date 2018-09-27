using Discord.Rest;

namespace GameToRole.Games
{
    class GameEntry
    {
        public string Name;
        public ulong DiscordRoleId;

        /// <summary>
        /// Creates a new GameEntry object with the name of the game and the RoleID
        ///     It also looks to see if the game already exists, and uses that instead
        /// </summary>
        /// <param name="name"></param>
        /// <param name="discordRole"></param>
        public GameEntry(string name, ulong discordRoleID)
        {
            Name = name;
            DiscordRoleId = discordRoleID;
        }
    }
}
