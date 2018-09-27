using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GameToRole.Games;

namespace GameToRole.Admin.Commands
{
    class DeleteAllRoles
    {
        [Command("delete_all_roles", "Deletes all roles that the bot has added when guild users play games\r\nOptional -force parameter deletes ALL rules that only have mention set (USE WITH CAUTION)")]
        [Permissions(Permissions.PermissionTypes.Administrator)]
        public async void DeleteAllGameRoles(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length.Equals(3))
            {
                if (parameters[2].Equals("-force", StringComparison.CurrentCultureIgnoreCase))
                    await DeleteAllRolesUsingForced(sktMessage, discordSocketClient);
                else
                {
                    await sktMessage.Channel.SendMessageAsync($"The parameter {parameters[2]} is unknown");
                }
            }
            else
                await DeleteAllRolesUsingDatabase(sktMessage, discordSocketClient);
        }

        private async Task DeleteAllRolesUsingForced(SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            await sktMessage.Channel.SendMessageAsync("The 'force' parameter is not yet implemented");
        }

        private async Task DeleteAllRolesUsingDatabase(SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.Author is IGuildUser _guildUser)
            {
                var discordGuild = discordSocketClient.GetGuild(_guildUser.GuildId);
                if (discordGuild == null)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        "Unable to obtain the Guild from you, don't know what went wrong - tell AlienX");
                    return;
                }

                var gameEntriesCopy = GameManager.Instance.GetGameEntries().ToList();

                foreach (var gameEntry in gameEntriesCopy)
                {
                    await GameManager.Instance.DeleteGameEntry(gameEntry, discordGuild);
                }
            }
            else
            {
                await sktMessage.Channel.SendMessageAsync(
                    "Unable to delete roles, you MUST run this command from the guild that you want it to apply to, you cannot PM me for this");
            }
        }
    }
}
