using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;
using PluginManager;

namespace ASCIIArt
{
    [Export(typeof(IPlugin))]
    public class ASCIIArtPlugin : IPlugin
    {
        public EventRouter EventRouter { get; set; }

        public string Name => "ASCIIArt";

        public string Description => "Renders text to ascii art.";

        public void ExecutePlugin()
        {
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.ASCIIArt>();
        }

        public void Dispose()
        {

        }
    }
}
