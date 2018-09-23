namespace HomeLabReporting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Threading.Tasks;
    using Discord.WebSocket;
    using PluginInterface;

    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        string IPlugin.Name => "AlienX's HomeLab Reporting";
        public DiscordSocketClient DiscordClient { get; set; }
        public List<string> Commands { get; set; }
        List<PluginRequestTypes.PluginRequestType> IPlugin.RequestTypes =>
            new List<PluginRequestTypes.PluginRequestType> { PluginRequestTypes.PluginRequestType.COMMAND };

        public void ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"HomeLabReporting.dll Plugin Loading...");
            SNMP.SnmpCommunication.Instance.StartCapture();
            Commands = SNMP.SnmpCommunication.Instance.GetCommandList();

            // Start our trap receiver too
            SNMP.TrapReceiver.Instance.SetDiscordSocketClient(DiscordClient);
        }

        public async Task CommandAsync(string command, string message, SocketMessage sktMessage)
        {
            // Check if it's an SNMP command
            await SNMP.SnmpCommunication.Instance.CommandExecute(command, message, sktMessage);
        }

        public Task Message(string message, SocketMessage sktMessage)
        {
            return Task.CompletedTask;
        }

        void IPlugin.Dispose()
        {
            //SNMP.SnmpCommunication.Instance.Dispose();
            GlobalLogger.Logger.Instance.WriteConsole("HomeLabReporting Disposed");
        }
    }
}
