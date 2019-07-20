using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using PluginManager;

namespace Interface
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void ExecutePlugin();
        void Dispose();
        PluginManager.EventRouter EventRouter { get; set; }
    }

    public interface IPluginWithRouter : IPlugin
    {
        PluginRouter PluginRouter { get; set; }
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