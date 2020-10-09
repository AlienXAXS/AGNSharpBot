using System.ComponentModel.Composition;
using CatDog.Commands;
using CommandHandler;
using Interface;
using PluginManager;

namespace CatDog
{
    [Export(typeof(IPlugin))]
    public class CatDogPlugin : IPluginWithRouter
    {
        public EventRouter EventRouter { get; set; }
        public PluginRouter PluginRouter { get; set; }

        string IPlugin.Name => "CatDog";

        string IPlugin.Description =>
            "Allows the use of the !cat and !dog commands which show random fluffy images of cats and dogs upon request.";

        void IPlugin.ExecutePlugin()
        {
            HandlerManager.Instance.RegisterHandler<CatCommand>();
            HandlerManager.Instance.RegisterHandler<DogCommand>();
        }

        void IPlugin.Dispose()
        {
        }
    }
}