using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using SnmpSharpNet;
using Newtonsoft.Json;

namespace HomeLabReporting.SNMP
{
    class SnmpCommunication
    {
        private static SnmpCommunication _instance;
        public static SnmpCommunication Instance = _instance ?? (_instance = new SnmpCommunication());

        private readonly List<SnmpHost> _snmpHosts = new List<SnmpHost>();

        private bool _stopCapture;

        public SnmpCommunication()
        {
            // Load the config file
            try
            {
                AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true)
                    .SetRetentionOptions(new RetentionOptions() {Compress = true, Days = 1});

                var config =
                    JsonConvert.DeserializeObject<List<SnmpHost>>(
                        System.IO.File.ReadAllText("Plugins\\Config\\HomeLabReporting.json"));
                _snmpHosts = config;
                TrapReceiver.Instance.OnTrapReceived += RaisedOnTrapReceived;
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Unable to parse JSON file for SNMP Communication\r\n{ex.Message}");
            }
        }

        private void RaisedOnTrapReceived(object sender, IpAddress ipAddress, VbCollection snmpVbCollection)
        {
            var snmpHost = _snmpHosts.FirstOrDefault(x => x.IpAddress ==
                (ipAddress.ToString().Contains(":") ? ipAddress.ToString().Split(':')[0] : ipAddress.ToString()));

            if (snmpHost == null)
            {
                var oidStrings = "";
                foreach (var x in snmpVbCollection)
                    oidStrings += $"{x.Oid} = {x.Value}\r\n";

                AdvancedLoggerHandler.Instance.GetLogger().Log($"[SNMP TRAP] Received snmp trap from an unknown source\r\nIP Address:{ipAddress}\r\n{oidStrings}");

                return;
            }

            var foundTrap = false;
            foreach (var snmpVb in snmpVbCollection)
            {
                foreach (var trapDefinition in snmpHost.SnmpHostTraps)
                {
                    // Do we have a match?
                    if (snmpVb.Oid.ToString().Equals(trapDefinition.Oid))
                    {
                        trapDefinition.LastValue = new SnmpHostValueDefinition(snmpVb.Value.ToString());
                        //await Logger.Instance.Log($"[SNMP TRAP] From {snmpHost.Name} -['{trapDefinition.ReadableName}' has a value of '{trapDefinition.LastValue.Value}']-", Logger.LoggerType.ConsoleAndDiscord, Logger.Instance.NewDiscordMention(trapDefinition.MentionSettings.UserId, trapDefinition.MentionSettings.GuildId, trapDefinition.MentionSettings.ChannelId));
                        foundTrap = true;
                    }
                }
            }

            if (!foundTrap)
            {
                var oidStrings = "";
                foreach (var x in snmpVbCollection)
                    oidStrings += $"{x.Oid} = {x.Value}\r\n";

                AdvancedLoggerHandler.Instance.GetLogger().Log($"[SNMP TRAP] Received snmp trap from {snmpHost.Name}, but the OID was not known\r\nIP Address:{ipAddress}\r\n{oidStrings}");
            }
        }

        public void Dispose()
        {
            _stopCapture = true;
        }

        public async void StartCapture()
        {
            // A crappy timer!

            var _tickDelay = 1000;

            while (!_stopCapture)
            {
                foreach (var snmpHostEntry in _snmpHosts)
                {
                    // Minus the delay off our next polling interval
                    snmpHostEntry.PollIntervalNext = snmpHostEntry.PollIntervalNext - _tickDelay;

                    if (snmpHostEntry.PollIntervalNext <= 0)
                    {
                        snmpHostEntry.PollIntervalNext =
                            snmpHostEntry.PollInterval; // Reset the poll interval back, so we tick next time
                        snmpHostEntry.Execute();
                    }
                }

                await Task.Delay(_tickDelay);
            }
        }

        public List<string> GetCommandList()
        {
            var list = new List<string>();
            foreach (var entry in _snmpHosts)
            {
                list.Add(entry.Command);
            }

            return list;
        }

        public async Task CommandExecute(string[] parameters, SocketMessage sktMessage)
        {
            var message = sktMessage.Content;

            if (parameters.Length != 2)
            {
                await sktMessage.Channel.SendMessageAsync($"Error: Parameters are wrong");
                return;
            }

            var command = parameters[1];

            foreach (var entry in _snmpHosts)
            {
                if (entry.Command.ToLower().Equals(command.ToLower()))
                {
                    // If we have any record to speak of
                    if (entry.LastContacted != DateTime.MinValue)
                    {
                        // Build our response
                        var builder = new EmbedBuilder();
                        builder.WithTitle(message.Contains("debug") ? $"{entry.Name} [DEBUG MODE ON]" : entry.Name);

                        foreach (var oidEntry in entry.OidList)
                        {
                            if (message.Contains("debug"))
                            {
                                if (oidEntry.Expression == null)
                                {
                                    builder.AddField($"{oidEntry.ReadableName} | OID: {oidEntry.Oid} |", oidEntry.GetFormattedValue());
                                }
                                else
                                {
                                    builder.AddField($"{oidEntry.ReadableName} | Expression: {oidEntry.Expression} |", oidEntry.GetFormattedValue());
                                }
                            }
                            else
                            {
                                builder.AddField(oidEntry.ReadableName, oidEntry.GetFormattedValue());
                            }
                        }

                        var timespan = DateTime.Now - entry.LastContacted;
                        var timespanString = "";
                        if (timespan.Minutes > 0)
                            timespanString += $"{timespan.Minutes}:";
                        if (timespan.Seconds > 0)
                            timespanString += $"{timespan.Seconds:D} seconds ago";
                        if (timespan.Minutes == 0 && timespan.Seconds == 0)
                            timespanString = "just now";

                        builder.AddField("Data Timestamp",
                            $"{entry.LastContacted} ({timespanString})");

                        await sktMessage.Channel.SendMessageAsync("", false, builder.Build());
                    }
                    else
                    {
                        await sktMessage.Channel.SendMessageAsync(
                            $"Sorry, I do not yet have any data for {entry.Name}, please try again later - I poll this device every {(entry.PollInterval / 1000)} seconds");
                    }
                }
            }
        }
    }

    public class Exceptions
    {
        public class ErrorInSnmpResponse : Exception
        {
            public ErrorInSnmpResponse(int errorStatus, int errorIndex) : base(string.Empty)
            {
            }
        }

        public class RequestedSnmpOIDsLargerThanResponse
        {
        }
    }
}
