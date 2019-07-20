using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
