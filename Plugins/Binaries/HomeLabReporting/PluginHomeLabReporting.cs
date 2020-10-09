using System.ComponentModel.Composition;
using CommandHandler;
using HomeLabReporting.SNMP;
using Interface;
using PluginManager;

namespace HomeLabReporting
{
    [Export(typeof(IPlugin))]
    public sealed class PluginHomeLabReporting : IPlugin
    {
        string IPlugin.Name => "AlienX's HomeLab Reporting";
        public string Description => "Homelab Reporting for AlienX's House";
        public EventRouter EventRouter { get; set; }

        public void ExecutePlugin()
        {
            SnmpCommunication.Instance.StartCapture();
            HandlerManager.Instance.RegisterHandler<Commands>();
        }

        void IPlugin.Dispose()
        {
            SnmpCommunication.Instance.Dispose();
        }
    }
}