using Interface;
using PluginManager;

namespace HomeLabReporting
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public sealed class PluginHomeLabReporting : IPlugin
    {
        string IPlugin.Name => "AlienX's HomeLab Reporting";
        public string Description => "Homelab Reporting for AlienX's House";
        public EventRouter EventRouter { get; set; }

        public void ExecutePlugin()
        {
            SNMP.SnmpCommunication.Instance.StartCapture();
            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands>();
        }

        void IPlugin.Dispose()
        {
            SNMP.SnmpCommunication.Instance.Dispose();
        }
    }
}