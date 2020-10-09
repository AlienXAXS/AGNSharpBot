using System;
using System.Collections.Generic;
using System.Linq;
using Auditor.WebServer.Configuration;
using CommandHandler;
using Discord;
using Discord.WebSocket;
using InternalDatabase;

namespace Auditor.WebServer
{
    internal class ControllerCommands
    {
        [Command("auditor", "Controls the Auditor plugin.")]
        public async void AuditorPublicCmd(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (!NancyServer.Instance.GetServerRunning())
            {
                await sktMessage.Channel.SendMessageAsync(
                    $"Sorry {sktMessage.Author.Username} but while the Auditor is logging for your discord server, the web server has not been configured yet.");
                return;
            }

            if (parameters.Length != 2)
            {
                await sktMessage.Channel.SendMessageAsync(
                    "Only command this has right now is !auditor `login` which requests a new login session to be used with Nancy");
                return;
            }

            if (parameters[1].Equals("login"))
            {
                var random = new Random(DateTime.Now.Millisecond);
                var authKey = new string(Enumerable
                    .Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 16)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                var wsConfig = ConfigHandler.Instance.Configuration;
                var authDb = Handler.Instance.GetConnection().DbConnection
                    .Table<AuditorSql.AuditorNancyLoginSession>();

                if (sktMessage.Channel is SocketGuildChannel socketGuildChannel)
                {
                    // Remove all old known keys that this user once had
                    foreach (var result in authDb.ToArray())
                        if (result.UserId == (long) sktMessage.Author.Id)
                            authDb.Connection.Delete(result);

                    authDb.Connection.Insert(new AuditorSql.AuditorNancyLoginSession
                    {
                        AuthKey = authKey,
                        GuildId = (long) socketGuildChannel.Guild.Id,
                        UserId = (long) sktMessage.Author.Id
                    });

                    await sktMessage.Author.SendMessageAsync(
                        $"AGNSharpBot Web Login Token: {wsConfig.URI}/login?key={authKey}");
                }
            }
        }

        [Command("auditoradmin", "Controls the Auditor plugin - try !auditoradmin help")]
        public async void AuditorAdmin(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length < 2)
            {
                await sktMessage.Channel.SendMessageAsync("Invalid use of command, try !auditoradmin help");
                return;
            }

            switch (parameters[1])
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("`Auditor Admin Help`\r\n" +
                                                              "`!auditoradmin help` - This help\r\n" +
                                                              "`!auditoradmin webserver help` - web server control");
                    break;

                case "webserver":
                    try
                    {
                        if (sktMessage.Channel is SocketGuildChannel sktGuildChannel)
                        {
                            var wsCmdResult = WebServerCommand(parameters.Skip(2), sktGuildChannel.Guild.Id);
                            await sktMessage.Channel.SendMessageAsync(wsCmdResult.Message);
                        }
                        else
                        {
                            await sktMessage.Channel.SendMessageAsync(
                                "Unable to process request as this channel doesnt seem to be in a guild");
                        }
                    }
                    catch (Exception ex)
                    {
                        await sktMessage.Channel.SendMessageAsync(ex.Message);
                    }

                    break;
            }
        }

        private ReturnValue WebServerCommand(IEnumerable<string> parameters, ulong guildId)
        {
            var paramArray = parameters as string[] ?? parameters.ToArray();
            if (paramArray.Count().Equals(0))
                return new ReturnValue(true, "Invalid parameters for webserver command, try help");

            switch (paramArray.ElementAt(0))
            {
                case "help":
                    return new ReturnValue(true, "`Auditor WebServer Help`\r\n" +
                                                 "`!auditoradmin webserver help` - This help\r\n" +
                                                 "`!auditoradmin webserver start` - Starts the web server\r\n" +
                                                 "`!auditoradmin webserver stop` - Stops the web server");

                case "start":
                    try
                    {
                        NancyServer.Instance.Start(true);
                        return new ReturnValue(false,
                            $"NancyServer started successfully, you can access it via {ConfigHandler.Instance.Configuration.URI}");
                    }
                    catch (Exception ex)
                    {
                        return new ReturnValue(true, $"Unable to start NancyServer, message was: {ex.Message}");
                    }

                case "stop":
                    try
                    {
                        NancyServer.Instance.Stop();
                        return new ReturnValue(false, "NancyServer stopped successfully");
                    }
                    catch (Exception ex)
                    {
                        return new ReturnValue(true, $"Unable to stop NancyServer, message was: {ex.Message}");
                    }

                default:
                    return new ReturnValue(true, "Unknown command, use help");
            }

            return null;
        }

        private class ReturnValue
        {
            public ReturnValue(bool failure, string message)
            {
                Failure = failure;
                Message = message;
            }

            public bool Failure { get; }
            public string Message { get; }
        }
    }
}