using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace PluginInterface
{
    public interface IPlugin
    {
        string Name { get; }
        void ExecutePlugin();

        DiscordSocketClient DiscordClient { get; set; }
        List<string> Commands { get; }
        List<PluginRequestTypes.PluginRequestType> RequestTypes { get; }

        Task CommandAsync(string command, string message, SocketMessage sktMessage);
        Task Message(string message, SocketMessage sktMessage);

        void Dispose();
    }

    public class PluginRequestTypes
    {
        public enum PluginRequestType
        {
            DISCONNECTED,
            CONNECTED,
            MESSAGE,
            COMMAND
        }
    }
}