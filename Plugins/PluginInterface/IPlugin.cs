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