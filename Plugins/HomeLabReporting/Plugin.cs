namespace HomeLabReporting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Threading.Tasks;
    using Discord.WebSocket;
    using PluginInterface;

    [Export(typeof(IPlugin))]
    public sealed class Plugin : IPlugin
    {
        string IPlugin.Name => "AlienX's HomeLab Reporting";
        public DiscordSocketClient DiscordClient { get; set; }

        public void ExecutePlugin()
        {
            GlobalLogger.Logger.Instance.WriteConsole($"HomeLabReporting.dll Plugin Loading...");
            SNMP.SnmpCommunication.Instance.StartCapture();

            // Start our trap receiver too
            SNMP.TrapReceiver.Instance.SetDiscordSocketClient(DiscordClient);
            DiscordClient.MessageReceived += DiscordClientOnMessageReceived;
        }

        private Task DiscordClientOnMessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        public async Task CommandAsync(string command, string message, SocketMessage sktMessage)
        {
            // Check if it's an SNMP command
            await SNMP.SnmpCommunication.Instance.CommandExecute(command, message, sktMessage);
        }

        void IPlugin.Dispose()
        {
            //SNMP.SnmpCommunication.Instance.Dispose();
            GlobalLogger.Logger.Instance.WriteConsole("HomeLabReporting Disposed");
        }
    }
}