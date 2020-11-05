using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;
using GlobalLogger;

namespace CatDog.Commands
{
    internal class DogCommand
    {
        public async Task<Stream> GetDogPictureAsync(bool gif)
        {
            try
            {
                var http = new HttpClient();
                var url = gif
                    ? "http://www.agngaming.com/private/agnsharpbot/catdog.gif"
                    : "http://www.agngaming.com/private/agnsharpbot/catdog.jpg";
                var resp = await http.GetAsync(url);
                return await resp.Content.ReadAsStreamAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [Command("dog", "Posts an image of a dog into the channel you put the command in.")]
        [Alias("dog", "woof", "doggo")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void Dog(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            try
            {
                var sktGuildUser = (SocketGuildUser) sktMessage.Author;
                var nickname = sktGuildUser.Nickname ?? sktGuildUser.Username;

                if (ContainsUnicodeCharacter(nickname))
                    nickname = sktGuildUser.Username;

                var gif = sktMessage.Content.ToLower().Contains("gif");

                var message =
                    await sktMessage.Channel.SendMessageAsync(
                        "Obtaining a cute fluffy image for you now, please wait...");

                var stream = await GetDogPictureAsync(gif);
                if (stream == null)
                    await message.ModifyAsync(properties =>
                        properties.Content = "Unable to connect to the Dog Service, try again later");
                else
                    try
                    {
                        await message.DeleteAsync();
                        await sktMessage.Channel.SendFileAsync(stream, gif ? "dog.gif" : "dog.jpg");
                    }
                    catch (Exception ex)
                    {
                        await message.ModifyAsync(properties =>
                            properties.Content =
                                $"Unable to get the image for you, perhaps the dog sat on the network cable... Error is:\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                    }
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log($"Error while attempting to get a doggo", Log4NetHandler.LogLevel.ERROR, exception:ex);
            }
        }

        public bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
    }
}