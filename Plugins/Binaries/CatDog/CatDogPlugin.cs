using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        string IPlugin.Description => "Allows the use of the !cat and !dog commands which show random fluffy images of cats and dogs upon request.";

        void IPlugin.ExecutePlugin()
        {
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.CatCommand>();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands.DogCommand>();
        }

        void IPlugin.Dispose()
        {
            
        }

        
    }
}

