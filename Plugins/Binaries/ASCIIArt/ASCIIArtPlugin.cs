using System.ComponentModel.Composition;
using CommandHandler;
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
            HandlerManager.Instance.RegisterHandler<Commands.ASCIIArt>();
        }

        public void Dispose()
        {
        }
    }
}