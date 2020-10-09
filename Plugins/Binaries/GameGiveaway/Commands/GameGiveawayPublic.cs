using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordMenu;
using CommandHandler;
using Discord;

namespace GameGiveaway.Commands
{
    internal class GameGiveawayHumbleMenu
    {
        public SocketMessage SktMessage { get; set; }
        public DiscordSocketClient DiscordSocketClient { get; set; }
        private bool _isFinished;
        private const double GiveawayAccessDurationInDays = 30.0;
        private MenuHandler _menu = new MenuHandler();

        public async void StartMenu()
        {
            var usersDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<Responses.Commands.GameGiveaway.SQL.GameGiveawayUserDb>();
            var dbUser = usersDb.DefaultIfEmpty(null).FirstOrDefault(x => x != null && x.DiscordId.Equals((long)SktMessage.Author.Id));

            _menu.Author = SktMessage.Author;
            _menu.DiscordSocketClient = DiscordSocketClient;
            _menu.DiscordSocketGuildChannel = SktMessage.Channel;

            // Humble check
            if (dbUser != null)
            {
                if (dbUser.isHumbleRegistered)
                {
                    await SktMessage.Channel.SendMessageAsync(
                        "Sorry, but only non humble monthly registered people can use this service.");
                    return;
                }
                else
                {
                    MenuOnOnMenuOptionSelected(_menu, new MenuOption(1, "No", "no"));
                    return;
                }
            }

            _menu.Init();
            _menu.MenuTitle = "Are you Humble Monthly Registered?";
            _menu.AddOption("Yes", "yes");
            _menu.AddOption("No", "no");
            _menu.Render(SktMessage.Channel);

            _menu.OnMenuOptionSelected += MenuOnOnMenuOptionSelected;

            var _myTask = new Task(async () =>
            {
                var timeout = 30;
                while (timeout > 0)
                {
                    timeout--;
                    await Task.Delay(1000);
                }

                if (!_isFinished)
                    _menu.Dispose("Menu has timed out");
            });
            _myTask.Start();
        }

        private async void MenuOnOnMenuOptionSelected(object selfMenu, MenuOption menuoption)
        {
            try
            {
                var menu = (MenuHandler)selfMenu;
                var gamesDbConnection = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<Responses.Commands.GameGiveaway.SQL.GameGiveawayGameDb>();
                var gamesDb = gamesDbConnection.Where(x => x.Used == false);
                var usersDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<Responses.Commands.GameGiveaway.SQL.GameGiveawayUserDb>();
                var dbUser = usersDb.DefaultIfEmpty(null).LastOrDefault(x => x != null && x.DiscordId.Equals((long)SktMessage.Author.Id));

                _menu.Dispose();

                if (dbUser != null)
                {
                    dbUser.isHumbleRegistered = menuoption.Metadata.Equals("yes");
                    usersDb.Connection.Update(dbUser);
                }
                else
                {
                    usersDb.Connection.Insert(new Responses.Commands.GameGiveaway.SQL.GameGiveawayUserDb()
                    {
                        DateTime = DateTime.Now,
                        DiscordId = (long)SktMessage.Author.Id,
                        isHumbleRegistered = menuoption.Metadata.Equals("yes")
                    });
                }

                if (menuoption.Metadata.Equals("yes"))
                {
                    await SktMessage.Channel.SendMessageAsync(
                        $"Thanks {SktMessage.Author.Username}, your preferences have been saved");

                    return;
                }

                if (!gamesDb.Any())
                {
                    await SktMessage.Channel.SendMessageAsync(
                        "Sorry, I do not have any free games to give away at the moment, try again later though!");
                    return;
                }

                // Time check
                if (dbUser != null)
                {
                    if (dbUser.isHumbleRegistered)
                    {
                        await SktMessage.Channel.SendMessageAsync(
                            $"Sorry, but Humble Monthly registered users cannot use this service");
                        return;
                    }

                    var lastAccessTimespan = (dbUser.DateTime.AddDays(GiveawayAccessDurationInDays) - DateTime.Now);
                    if (lastAccessTimespan.TotalDays > 0)
                    {
                        await SktMessage.Channel.SendMessageAsync(
                            $"Sorry {SktMessage.Author.Username}, but you'll have to wait {Util.ReadableTimespan.GetReadableTimespan(lastAccessTimespan)} before you can claim another game.");
                        return;
                    }
                }

                // Code will only reach here if the user either isn't in the claims database, or if they didn't claim within the last GiveawayAccessDurationInDays value

                // Insert our user into the database
                if (dbUser == null)
                    usersDb.Connection.Insert(new Responses.Commands.GameGiveaway.SQL.GameGiveawayUserDb() { DiscordId = (long)SktMessage.Author.Id, DateTime = DateTime.Now });
                else
                {
                    dbUser.DateTime = DateTime.Now;
                    usersDb.Connection.Update(dbUser);
                }

                // Grab a random game from the database, modify it to be marked as used and send a PM to the user with the key.
                var rand = new Random(DateTime.Now.Millisecond);
                var givenGame = gamesDb.ElementAt(rand.Next(gamesDb.Count()));
                givenGame.Used = true;
                gamesDb.Connection.Update(givenGame);

                await SktMessage.Author.SendMessageAsync(
                    $"Free Games - By AlienX's Gaming Network\r\n\r\nYour Free Game:\r\nName: `{givenGame.Name}`\r\nKey: `{givenGame.Key}`\r\n\r\nPlease activate this game on steam (unless specified otherwise).");

                await SktMessage.Channel.SendMessageAsync($"Free Games - By AlienX's Gaming Network\r\n" +
                                                          $"Congratulations {SktMessage.Author.Username}!  -  You have been given a free copy of `{givenGame.Name}`." +
                                                          $"\r\n" +
                                                          $"Claim your own free game - simply type !gamegiveaway (or !gg for short) in chat.");
            }
            catch (Exception ex)
            {
                await SktMessage.Channel.SendMessageAsync(
                    $"Sorry, an error stopped you from getting your free game. Please report this to AlienX.\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }
    }

    internal class GameGiveawayPublic
    {
        [Command("gamegiveaway", "Game Giveaway - Get free Steam Keys")]
        [Alias("gg", "ggz")]
        public async void GameGiveawayCmd(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            var author = sktMessage.Author;
            if (author is SocketGuildUser guildUser)
            {
                var joinedRecently = (DateTime.Now - guildUser.JoinedAt);
                if (joinedRecently.Value.TotalDays < 30)
                {
                    await sktMessage.Channel.SendMessageAsync($"Sorry {author.Username}, but you have to be active within the community in order to use this feature.");
                    return;
                }
            }

            var giveaway = new GameGiveawayHumbleMenu
            {
                SktMessage = sktMessage,
                DiscordSocketClient = discordSocketClient
            };
            giveaway.StartMenu();
        }
    }
}