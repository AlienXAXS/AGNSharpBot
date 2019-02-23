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
    class CatCommand
    {
        public async Task<Stream> GetCatPictureAsync(string text, bool gif)
        {
            HttpClient http = new HttpClient();
            var url = gif ? "https://cataas.com/cat/gif/says/" + text : $"https://cataas.com/cat/says/" + text;
            var resp = await http.GetAsync(url);
            return await resp.Content.ReadAsStreamAsync();
        }

        [Command("cat", "Posts an image of a cat into the channel you put the command in - you can also request a GIF by saying !cat gif")]
        [Alias("kitty", "pussy", "meow")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void Cat(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var sktGuildUser = ((SocketGuildUser) sktMessage.Author);
            var nickname = sktGuildUser.Nickname ?? sktGuildUser.Username;

            if (ContainsUnicodeCharacter(nickname))
                nickname = sktGuildUser.Username;

            var gif = sktMessage.Content.ToLower().Contains("gif");
            var stream = await GetCatPictureAsync($"A Kitty For {nickname}", gif);

            await sktMessage.Channel.SendFileAsync(stream, (gif ? "cat.gif" : "cat.png"));
        }

        public bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
    }
}