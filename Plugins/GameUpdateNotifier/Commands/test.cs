using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;
using HtmlAgilityPack;

namespace GameUpdateNotifier.Commands
{
    class test
    {
        [Command("test", "Test cmd.")]
        public async void TestCmd(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var web = new HtmlWeb();
            var document = web.Load("https://playoverwatch.com/en-us/news/patch-notes/pc");

            var patchNodes = document.DocumentNode.SelectNodes(@"//div[@class='PatchNotesSideNav']/ul/li");

            await sktMessage.Channel.SendMessageAsync($"Found {patchNodes.Count} overwatch patches");
            foreach (var node in patchNodes)
            {
                var patchNumber = node.ChildNodes.FindFirst("a").InnerText;
                var patchHyperlink = $"https://playoverwatch.com/en-us/news/patch-notes/pc{node.ChildNodes.FindFirst("a")?.Attributes.First(x => x.Name.Equals("href")).Value}";
                var patchDate = node.ChildNodes.FindFirst("p").InnerText;
                await sktMessage.Channel.SendMessageAsync($"Overwatch Patch: {patchNumber} released on {patchDate} - {patchHyperlink}");
            }
        }
    }
}