using System.Reflection;

namespace PluginManager
{
    public class PluginRouter
    {
        public bool IsPluginExecutableOnGuild(ulong guildId)
        {
            var pluginName = Assembly.GetCallingAssembly().ManifestModule.ScopeName;
            var pluginHandler = PluginHandler.Instance;
            return pluginHandler.ShouldExecutePlugin(pluginName, guildId);
        }
    }
}