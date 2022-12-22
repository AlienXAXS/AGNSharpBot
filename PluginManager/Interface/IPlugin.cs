using PluginManager;

namespace Interface
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }

        string Version { get; }

        EventRouter EventRouter { get; set; }

        void ExecutePlugin();

        void Dispose();
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