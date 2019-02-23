using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace Responses.Commands.Global
{
    class DogCommand
    {
        public async Task<Stream> GetDogPictureAsync(bool gif)
        {
            HttpClient http = new HttpClient();
            var url = gif ? "http://www.agngaming.com/private/agnsharpbot/catdog.gif" : $"http://www.agngaming.com/private/agnsharpbot/catdog.jpg";
            var resp = await http.GetAsync(url);
            return await resp.Content.ReadAsStreamAsync();
        }

        [Command("dog", "Posts an image of a dog into the channel you put the command in - putting gif.")]
        [Alias("dog", "woof", "doggo")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void Dog(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var sktGuildUser = ((SocketGuildUser) sktMessage.Author);
            var nickname = sktGuildUser.Nickname ?? sktGuildUser.Username;

            if (ContainsUnicodeCharacter(nickname))
                nickname = sktGuildUser.Username;

            var gif = sktMessage.Content.ToLower().Contains("gif");
            var stream = await GetDogPictureAsync(gif);

            await sktMessage.Channel.SendFileAsync(stream, (gif ? "dog.gif" : "dog.jpg"));
        }

        public bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
    }
}