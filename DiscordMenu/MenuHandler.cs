using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordMenu
{
    public class MenuOption
    {
        public string Caption;
        public Emoji Emoji;
        public int Id;
        public string Metadata;

        public MenuOption(int id, string caption)
        {
            Id = id;
            Caption = caption;
            Metadata = null;
        }

        public MenuOption(int id, string caption, string metaData)
        {
            Id = id;
            Caption = caption;
            Metadata = metaData;
        }
    }

    public class MenuHandler
    {
        public delegate void MenuOptionSelected(object sender, MenuOption menuOption);

        private readonly List<MenuOption> MenuOptions = new List<MenuOption>();
        private RestUserMessage Message;
        public string MenuTitle { get; set; }
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public ISocketMessageChannel DiscordSocketGuildChannel { get; set; }
        public SocketUser Author { get; set; }

        public event MenuOptionSelected OnMenuOptionSelected;

        public void Init()
        {
            if (DiscordSocketClient == null)
                throw new Exception("DiscordSocket is not set");

            MenuOptions.Add(new MenuOption(-1, "Cancel Task"));
        }

        public MenuHandler AddOption(string caption, string metaData)
        {
            if (MenuOptions.Count == 10)
                throw new IndexOutOfRangeException("List is too large, can only contain 10 items");

            MenuOptions.Add(new MenuOption(MenuOptions.Count, caption, metaData));

            return this;
        }

        public MenuHandler AddOption(string caption)
        {
            if (MenuOptions.Count == 10)
                throw new IndexOutOfRangeException("List is too large, can only contain 10 items");

            MenuOptions.Add(new MenuOption(MenuOptions.Count, caption));

            return this;
        }

        public async void Render(ISocketMessageChannel guildChannel)
        {
            var rnd = new Random();

            DiscordSocketGuildChannel = guildChannel;

            var thisEmbed = new EmbedBuilder();
            thisEmbed.Color = new Color(rnd.Next(256), rnd.Next(256), rnd.Next(256));

            var queryMessage = $"{MenuTitle}{Environment.NewLine}{Environment.NewLine}";

            foreach (var item in MenuOptions.Where(x => x.Id != -1))
                queryMessage += $"{IdToEmote(item.Id)} `{item.Caption}`{Environment.NewLine}";

            thisEmbed.AddField("Query:", $"{queryMessage}{Environment.NewLine}");
            thisEmbed.AddField("Note", $"Add a reaction to use the menu, to cancel click the {IdToEmote(-1)} button");

            Message = await DiscordSocketGuildChannel.SendMessageAsync("", false, thisEmbed.Build());

            foreach (var item in MenuOptions)
                await Message.AddReactionAsync(new Emoji(IdToEmote(item.Id, getEmoji: true)));

            DiscordSocketClient.ReactionAdded += DiscordSocketClientOnReactionAdded;
        }

        private Task DiscordSocketClientOnReactionAdded(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> socketMessageChannel, SocketReaction reaction)
        {
            // Ensure that the person clicking reactions is the person who started this
            if (reaction.UserId != Author.Id) return Task.CompletedTask;

            var foundMenuOption = MenuOptions.Where(x => IdToEmote(x.Id, getEmoji: true).Equals(reaction.Emote.Name))
                .DefaultIfEmpty(null).FirstOrDefault();
            if (foundMenuOption == null)
                throw new Exception("Unable to find matching emote for clicked reaction - tell the developer");

            if (foundMenuOption.Id == -1)
                Dispose("User Canceled Task");
            else
                OnMenuOptionSelected?.Invoke(this, foundMenuOption);

            return Task.CompletedTask;
        }

        public async void Dispose(string disposeMessage = "")
        {
            if (Message == null) return;

            if (disposeMessage != "")
            {
                await Message.DeleteAsync();
                await DiscordSocketGuildChannel.SendMessageAsync(disposeMessage == ""
                    ? "Execution complete"
                    : disposeMessage);
            }

            DiscordSocketClient.ReactionAdded -= DiscordSocketClientOnReactionAdded;

            try
            {
                await Message.DeleteAsync();
            }
            catch (Exception)
            {
            }

            Message = null;
        }

        private string IdToEmote(int id, bool justName = false, bool getEmoji = false)
        {
            switch (id)
            {
                case 0:
                    if (getEmoji) return "0⃣";
                    return justName ? "zero" : ":zero:";

                case 1:
                    if (getEmoji) return "1⃣";
                    return justName ? "one" : ":one:";

                case 2:
                    if (getEmoji) return "2⃣";
                    return justName ? "two" : ":two:";

                case 3:
                    if (getEmoji) return "3⃣";
                    return justName ? "three" : ":three:";

                case 4:
                    if (getEmoji) return "4⃣";
                    return justName ? "four" : ":four:";

                case 5:
                    if (getEmoji) return "5⃣";
                    return justName ? "five" : ":five:";

                case 6:
                    if (getEmoji) return "6⃣";
                    return justName ? "six" : ":six:";

                case 7:
                    if (getEmoji) return "7⃣";
                    return justName ? "seven" : ":seven:";

                case 8:
                    if (getEmoji) return "8⃣";
                    return justName ? "eight" : ":eight:";

                case 9:
                    if (getEmoji) return "9⃣";
                    return justName ? "nine" : ":nine:";

                case 10:
                    if (getEmoji) return "🔟";
                    return justName ? "ten" : ":ten:";

                case -1:
                    if (getEmoji) return "🚫";
                    return justName ? "no entry" : ":no_entry_sign:";
            }

            if (getEmoji) return "⚠";

            return justName ? "warning" : ":warning:";
        }
    }
}