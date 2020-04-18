using GlobalLogger.AdvancedLogger;
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
            AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                .SetRetentionOptions(new RetentionOptions() { Compress = true });

            AdvancedLoggerHandler.Instance.GetLogger().Log($"HomeLabReporting.dll Plugin Loading...");
            SNMP.SnmpCommunication.Instance.StartCapture();

            CommandHandler.HandlerManager.Instance.RegisterHandler<Commands>();
        }

        void IPlugin.Dispose()
        {
            SNMP.SnmpCommunication.Instance.Dispose();
            AdvancedLoggerHandler.Instance.GetLogger().Log("HomeLabReporting Disposed");
        }
    }
}