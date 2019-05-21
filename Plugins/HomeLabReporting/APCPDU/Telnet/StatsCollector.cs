using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using SimpleTCP;

namespace HomeLabReporting.APCPDU.Telnet
{

    class StatsCollected
    {
        public string Uptime { get; set; }
        public string OutputCurrent { get; set; }
        public string TotalPower { get; set; }
    }

    class StatsCollector
    {
        public void GetStats()
        {
            var tcpClient = new SimpleTcpClient();
            tcpClient.Connect("172.16.0.12", 23);

            var apcUptime = "";
            var apcOutputCurrent = "";
            var apcTotalPower = "";

            var cmds = 0;

            tcpClient.Delimiter = 10;
            tcpClient.DelimiterDataReceived += (sender, tcpClientResponse) =>
            {
                try
                {
                    if (tcpClientResponse.MessageString == "") return;

                    // UPTIME
                    if (tcpClientResponse.MessageString.Contains("Up Time"))
                    {
                        var regex = new Regex("Up Time   :[ \t]*([^\n\r]*)Stat");
                        var match = regex.Match(tcpClientResponse.MessageString);
                        if (match.Success && match.Groups.Count == 1)
                        {
                            apcUptime = match.Groups[1].Value.Trim();
                        }
                    }

                    // Total Output Current
                    if (tcpClientResponse.MessageString.Contains("Total Output Current"))
                    {
                        var regex = new Regex("\\bTotal Output Current\\s+:\\s+(.*)");
                        var match = regex.Match(tcpClientResponse.MessageString);
                        if (match.Success && match.Groups.Count == 1)
                        {
                            apcOutputCurrent = match.Groups[1].Value.Trim();
                        }
                    }

                    // Total watts
                    if (tcpClientResponse.MessageString.Contains("Total Power"))
                    {
                        var regex = new Regex("\\bTotal Power\\s +:\\s + (.*)");
                        var match = regex.Match(tcpClientResponse.MessageString);
                        if (match.Success && match.Groups.Count == 1)
                        {
                            apcTotalPower = match.Groups[1].Value.Trim();
                        }

                        var embedBuilder = new EmbedBuilder();
                        embedBuilder.Title = "AGN Gaming Home APC PDU Stats";
                        embedBuilder.AddField("UpTime", apcUptime, true);
                        embedBuilder.AddField("Current Load", apcOutputCurrent, true);
                        embedBuilder.AddField("Total Wattage Load", apcTotalPower, true);
                        embedBuilder.Timestamp = DateTimeOffset.Now;

                        var wattage = int.Parse(apcTotalPower.Substring(0,
                            apcTotalPower.IndexOf(" ", StringComparison.Ordinal)));

                        var apcTotalCost = ((double)wattage / 1000) * 24 * 0.13;
                        embedBuilder.AddField("Total Cost Per Day / Month", $"£{apcTotalCost} / £{apcTotalCost * 30}",
                            true);

                        //await sktMessage.Channel.SendMessageAsync("", false, embedBuilder.Build());
                        Console.WriteLine("Disconnecting");
                        tcpClient.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    //await sktMessage.Channel.SendMessageAsync($"Error while processing command:\r\n{ex.Message}");
                    tcpClient.Disconnect();
                }
            };

            tcpClient.DataReceived += (sender, tcpClientResponse) =>
            {

                cmds++;
                if (cmds > 100)
                {
                    //await sktMessage.Channel.SendMessageAsync(
                    //"Unable to read from TCPStream, perhaps navigating the telnet menus went wrong?");
                    Console.WriteLine("Too many responses... killing connection");
                    tcpClient.Disconnect();
                    return;
                }


                // USERNAME PROMPT
                if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(tcpClientResponse.MessageString, "User Name", CompareOptions.IgnoreCase) >= 0)
                {
                    tcpClientResponse.Reply("apc\r\n");
                    return;
                }

                // PASSWORD PROMPT
                if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(tcpClientResponse.MessageString, "Password",
                        CompareOptions.IgnoreCase) >= 0)
                {
                    tcpClientResponse.Reply("apc\r\n");
                    return;
                }

                // LOGGED IN
                if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(tcpClientResponse.MessageString,
                        "American Power Conversion", CompareOptions.IgnoreCase) >= 0)
                {
                    tcpClientResponse.Reply("1\r\n"); // devman
                    tcpClientResponse.Reply("6\r\n"); // measure
                }
            };
        }
    }
}
