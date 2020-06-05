using CommandHandler;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GlobalLogger;

namespace Responses.Commands.Global
{
    internal class CatCommand
    {
        public async Task<Stream> GetCatPictureAsync(string text, bool gif)
        {
            var http = new HttpClient();
            var url = gif ? "https://cataas.com/cat/gif/says/" + text : $"https://cataas.com/cat/says/" + text;
            var resp = await http.GetAsync(url);
            return await resp.Content.ReadAsStreamAsync();
        }

        [Command("cat", "Posts an image of a cat into the channel you put the command in - you can also request a GIF by saying !cat gif")]
        [Alias("kitty", "pussy", "meow")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void Cat(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var sktGuildUser = ((SocketGuildUser)sktMessage.Author);
            var nickname = sktGuildUser.Nickname ?? sktGuildUser.Username;

            if (ContainsUnicodeCharacter(nickname))
                nickname = sktGuildUser.Username;

            var gif = sktMessage.Content.ToLower().Contains("gif");

            var message =
                await sktMessage.Channel.SendMessageAsync("Obtaining a cute fluffy image for you now, please wait...");


            Stream stream;
            try
            {
                 stream = await GetCatPictureAsync($"A Kitty For {nickname}", gif);
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log("Cat Command Handler was unable to get a picture of a cat", Log4NetHandler.LogLevel.ERROR, exception:ex);
                await message.ModifyAsync(properties =>
                    properties.Content = "I'm sorry, but Cataas seems to be offline - Maybe try again later?");
                return;
            }

            var streamReader = new StreamReader(stream);
            var streamContents = streamReader.ReadToEnd();

            if (streamContents.Contains("504 Gateway Time-out"))
                stream = null;

            if (stream == null)
                await message.ModifyAsync(properties => properties.Content = "Unable to connect to the Cat Service, try again later");
            else
            {
                try
                {
                    await message.DeleteAsync();
                    await sktMessage.Channel.SendFileAsync(stream, (gif ? "cat.gif" : "cat.png"));
                }
                catch (Exception ex)
                {
                    await message.ModifyAsync(properties =>
                        properties.Content =
                            $"Unable to get the image for you, perhaps the cat sat on the network cable... Error is:\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                }
            }
        }

        public bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
    }
}