using System;
using CommandHandler;
using Discord.WebSocket;
using PUBGWeekly.Game;

namespace PUBGWeekly
{
    internal class UserCommandHandler
    {
        [Command("pubgweekly", "The primary command for PUBG Weekly Utiltiies - try !pubgweekly help")]
        [Alias("pgw", "pubg")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void CommandHandler(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 0) return;

            switch (parameters[1].ToLower())
            {
                case "join":
                    if (!GameHandler.Instance.IsLive)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Sorry {sktMessage.Author.Username}, there is no PUBG Weekly Event live right now");
                        return;
                    }

                    try
                    {
                        GameHandler.Instance.NewPlayer(sktMessage.Author.Username, sktMessage.Author.Id);
                        await sktMessage.Channel.SendMessageAsync(
                            $"{sktMessage.Author.Username}, You've been added to this weeks PUBG Weekly Event!");
                    }
                    catch (ExceptionOverloads.PlayerAlreadyRegistered)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            "You're already part of this weeks PUBG Weekly Games!");
                    }
                    catch (Exception ex)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Fatal error while trying to add you to the game: {ex.Message}\r\n\r\n{ex.StackTrace}");
                    }

                    break;

                case "leave":
                    try
                    {
                        GameHandler.Instance.RemovePlayer(sktMessage.Author.Id);
                        await sktMessage.Channel.SendMessageAsync(
                            "You were removed from the PUBG Weekly Player Listing");
                    }
                    catch (ExceptionOverloads.PlayerNotFound)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            "I can't find you, so I can't remove you! (you dumb dumb!)");
                    }

                    break;
            }
        }
    }
}