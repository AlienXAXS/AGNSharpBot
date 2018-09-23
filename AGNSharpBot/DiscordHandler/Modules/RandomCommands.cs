using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AGNSharpBot.DiscordHandler.Services;
using Discord.Commands;
using Discord.WebSocket;

namespace AGNSharpBot.DiscordHandler.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class RandomCommands : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        /// <summary>
        /// Kitty!
        ///     Gets a random picture of a cat and sends it to the channel where the command was issued
        /// </summary>
        /// <returns></returns>
        [Command("pussy")]
        [Alias("cat","kitty")]
        public async Task PussyAsync()
        {
            var stream = await PictureService.GetCatPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("help")]
        public async Task Help()
        {
            var x = (SocketGuildUser) Context.User;

            await Context.Channel.SendMessageAsync(
                $"{x.Nickname ?? x.Username}... There's no helping you now, YOU'RE DOOMED I TELL YOU!");
        }

        /// <summary>
        /// ChannelInfo
        ///     Get's the current channels info and responds to the channel where the command was issued
        /// </summary>
        /// <returns></returns>
        [Command("channelinfo")]
        public async Task ChannelInfo()
        {
            await Context.Channel.SendMessageAsync($"Channel ID is {Context.Channel.Id} which is in the guild {Context.Guild.Id}");
        }

        /// <summary>
        /// GetAllChannels
        ///     Get's all of the channel names and their respetive ID's and outputs them into the current channel where the command was issued
        /// </summary>
        /// <returns></returns>
        [Command("getallchannels")]
        public async Task GetAllChannels()
        {
            var channels = Context.Guild.Channels;
            await Context.Channel.SendMessageAsync(
                $"Channels i can see are: \r\n{channels.Aggregate("", (current, channel) => $"{current}\r\n{channel.Name} -> {channel.Id}")}");
        }
    }
}