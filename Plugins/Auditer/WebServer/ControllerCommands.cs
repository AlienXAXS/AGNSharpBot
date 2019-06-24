using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace Auditor.WebServer
{
    class ControllerCommands
    {
        private class ReturnValue
        {
            public bool Failure { get; set; }
            public string Message { get; set; }

            public ReturnValue(bool failure, string message)
            {
                Failure = failure;
                Message = message;
            }
        }

        [Command("auditoradmin", "Controls the Auditor plugin - try !auditoradmin help")]
        public async void AuditorAdmin(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length < 2)
            {
                await sktMessage.Channel.SendMessageAsync($"Invalid use of command, try !auditoradmin help");
                return;
            }

            switch (parameters[1])
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync($"`Auditor Admin Help`\r\n" +
                                                              $"`!auditoradmin help` - This help\r\n" +
                                                              $"`!auditoradmin webserver help` - web server control");
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
                                                          "`!auditoradmin webserver stop` - Stops the web server\r\n" +
                                                          "`!auditoradmin webserver configure` - Configures the web server.  Params: ip:port autostart uri");

                case "start":
                    break;

                case "stop":
                    break;

                case "configure":
                    var ipPortPair = paramArray.ElementAt(1);
                    var autoStart = paramArray.ElementAt(2).Equals("true") ? true : false;
                    var ipPortSplit = ipPortPair.Split(':');
                    var uri = paramArray.ElementAt(3);

                    if (!IPAddress.TryParse(ipPortSplit[0], out var ipAddress))
                        return new ReturnValue(true,"Invalid IP Address given");

                    if (!int.TryParse(ipPortSplit[1], out var port))
                        return new ReturnValue(true, "Invalid port given");

                    IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                    TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                    if (tcpConnInfoArray.Any(tcpi => tcpi.LocalEndPoint.Port == port))
                        return new ReturnValue(true, $"Sorry, port {port} is already in use, try another.");

                    Configuration.Instance.UpdateConfiguration(ipAddress, port, autoStart, uri, guildId);

                    return new ReturnValue(false, "Configuration saved, you can start the webserver now with !auditoradmin webserver start.");

                default:
                    return new ReturnValue(true, "Unknown command, use help");
            }

            return null;
        }
    }
}
