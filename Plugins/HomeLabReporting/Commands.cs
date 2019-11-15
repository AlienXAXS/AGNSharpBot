using CommandHandler;
using Discord.WebSocket;

namespace HomeLabReporting
{
    internal class Commands
    {
        [Command("home", "Get's home information, for AlienX's House :)")]
        public async void HomeCommand(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            await SNMP.SnmpCommunication.Instance.CommandExecute(parameters, sktMessage);
        }
    }
}