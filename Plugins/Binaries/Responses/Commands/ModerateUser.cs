using System;
using System.Linq;
using System.Threading.Tasks;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using DiscordMenu;

namespace Responses.Commands
{
    internal class ModerateUserSession : IDisposable
    {
        public delegate void SessionPreDispose();

        private MenuHandler _menuHandler;

        private bool IsFinished;
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public SocketMessage SocketMessage { get; set; }
        public SocketGuildUser Victim { get; set; }

        public void Dispose()
        {
            _menuHandler?.Dispose();
        }

        public event SessionPreDispose OnSessionPreDispose;

        public void Start()
        {
            _menuHandler = new MenuHandler
            {
                DiscordSocketClient = DiscordSocketClient,
                MenuTitle = $"Moderation Menu for user {Victim.Username}",
                Author = SocketMessage.Author
            };

            _menuHandler.Init();
            _menuHandler.AddOption("Kick");
            _menuHandler.AddOption("Ban");
            _menuHandler.AddOption("Automatically delete all future messages from this user");
            _menuHandler.AddOption("Block user from posting images / links");
            _menuHandler.AddOption("Block user from setting a nickname");
            _menuHandler.AddOption("Disallow user to talk in this channel");

            _menuHandler.OnMenuOptionSelected += MenuHandlerOnOnMenuOptionSelected;
            _menuHandler.Render(SocketMessage.Channel);

            var _myTask = new Task(async () =>
            {
                var timeout = 30;
                while (timeout > 0)
                {
                    timeout--;
                    await Task.Delay(1000);
                }

                if (!IsFinished)
                    _menuHandler.Dispose("Menu has timed out");
            });
            _myTask.Start();
        }

        private async void MenuHandlerOnOnMenuOptionSelected(object sender, MenuOption menuOption)
        {
            IsFinished = true;
            if (Victim.Roles.Any(x => x.Permissions.Administrator))
            {
                _menuHandler?.Dispose("You cannot do this against an administrator");
                OnSessionPreDispose?.Invoke();
                return;
            }

            switch (menuOption.Id)
            {
                case 1:
                    await Victim.KickAsync($"You were kicked by {SocketMessage.Author.Username}");
                    _menuHandler?.Dispose($"Victim {Victim.Username} was kicked by {SocketMessage.Author.Username}");
                    break;

                case 2:
                    await Victim.BanAsync(reason: $"You were banned by {SocketMessage.Author.Username}");
                    _menuHandler?.Dispose($"Victim {Victim.Username} was banned by {SocketMessage.Author.Username}");
                    break;

                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    _menuHandler?.Dispose(
                        $"{SocketMessage.Author.Username} picked a menu option which is not finished yet - Coming Soon!");
                    break;
            }
        }
    }

    internal class ModerateUser
    {
        [Command("mod", "!mod <userid> - Moderate a user using an interactive menu")]
        public async void MenuTest(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"{sktMessage.Author.Username} - Invalid use of userinfo, try !help");
                return;
            }

            if (sktMessage.Channel is SocketGuildChannel _socketGuild)
                if (ulong.TryParse(parameters[1], out var userId))
                {
                    // UID based search
                    var user = _socketGuild.Users.Where(x => x.Id == userId).DefaultIfEmpty(null).FirstOrDefault();
                    if (user == null)
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            "I am unable to find a user with that ID");
                        return;
                    }

                    var _myTask = new Task(async () =>
                    {
                        var sessionExpired = false;

                        using (var session = new ModerateUserSession
                        {
                            DiscordSocketClient = discordSocketClient,
                            SocketMessage = sktMessage,
                            Victim = user
                        })
                        {
                            session.Start();

                            session.OnSessionPreDispose += () => { sessionExpired = true; };

                            while (!sessionExpired) await Task.Delay(1000);
                        }
                    });
                    _myTask.Start();
                }
        }
    }
}