// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrapReceiver.cs" company="AGNGaming">
//   Copyright AGN Gaming -- Mark "AlienX" Phillips
// </copyright>
// <summary>
//   The trap receiver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger;
using Newtonsoft.Json;

namespace HomeLabReporting.SNMP
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    using SnmpSharpNet;

    class TrapReceiverConfiguration
    {
        public int Port { get; set; } = 162;
    }

    /// <summary>
    /// The trap receiver.
    /// </summary>
    public class TrapReceiver : IDisposable
    {
        private static TrapReceiver _instance;
        public static TrapReceiver Instance = _instance ?? (_instance = new TrapReceiver());

        private DiscordSocketClient _discordSocketClient;
        private TrapReceiverConfiguration _trapReceiverConfiguration;

        public delegate void EventRaiser(IpAddress ipAddress, VbCollection snmpVbCollection);

        public event EventRaiser OnTrapReceived;

        private const string ConfigurationPath = "Plugins\\Config\\Snmp.json";

        private readonly Task _trapReceiverTask;

        public TrapReceiver()
        {
            if (System.IO.File.Exists(ConfigurationPath))
            {
                try
                {
                    _trapReceiverConfiguration =
                        JsonConvert.DeserializeObject<TrapReceiverConfiguration>(
                            System.IO.File.ReadAllText(ConfigurationPath));
                }
                catch (Exception ex)
                {
    #pragma warning disable 4014
                    Logger.Instance.Log(
                        $"Unable to deserialize snmp.json, error is as follows:\r\n{ex.Message}",
                        Logger.LoggerType.ConsoleOnly);
    #pragma warning restore 4014
                }
            }
            else
            {
                try
                {
                    System.IO.File.WriteAllText(ConfigurationPath, JsonConvert.SerializeObject(
                        new TrapReceiverConfiguration(),
                        Formatting.Indented));
                }
                catch (Exception ex)
                {
    #pragma warning disable 4014
                    Logger.Instance.Log(
                        $"Exception while attempting to write to snmp.json, error is as follows: {ex.Message}",
                        Logger.LoggerType.ConsoleOnly);
    #pragma warning restore 4014
                }
            }

            _trapReceiverTask = Task.Run(() => StartTrapReceiver());
        }

        public void SetDiscordSocketClient(DiscordSocketClient discordSocketClient)
        {
            _discordSocketClient = discordSocketClient;
        }

        /// <summary>
        /// The thing.
        /// </summary>
        private void StartTrapReceiver()
        {
            // Construct a socket and bind it to the trap manager port 162 
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var ipep = new IPEndPoint(IPAddress.Any, 162);
            EndPoint ep = ipep;

            socket.Bind(ep);

            // Disable timeout processing. Just block until packet is received 
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            while (true)
            {
                var inData = new byte[16 * 1024];

                // 16KB receive buffer int inlen = 0;
                var peer = new IPEndPoint(IPAddress.Any, 0);
                EndPoint inEndPoint = peer;
                var inLength = -1;
                try
                {
                    inLength = socket.ReceiveFrom(inData, ref inEndPoint);

                    // Do not process this SNMP trap if we're not connected to discord
                    if (_discordSocketClient == null ||
                        _discordSocketClient?.ConnectionState == ConnectionState.Disconnected ||
                        _discordSocketClient?.ConnectionState == ConnectionState.Disconnecting)
                        continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex.Message);
                    inLength = -1;
                }

                if (inLength > 0)
                {
                    // Check protocol version int 
                    var ver = SnmpPacket.GetProtocolVersion(inData, inLength);
                    if (ver == (int) SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet 
                        var pkt = new SnmpV1TrapPacket();
                        pkt.decode(inData, inLength);

                        OnTrapReceived?.Invoke(pkt.Pdu.AgentAddress, pkt.Pdu.VbList);
                    }
                    else
                    {
                        // Parse SNMP Version 2 TRAP packet 
                        var pkt = new SnmpV2Packet();
                        pkt.decode(inData, inLength);
                        Console.WriteLine("** SNMP Version 2 TRAP received from {0}:", inEndPoint);
                        if (pkt.Pdu.Type != PduType.V2Trap)
                        {
                            Console.WriteLine("*** NOT an SNMPv2 trap ****");
                        }
                        else
                        {

                            Console.WriteLine("*** Community: {0}", pkt.Community);
                            Console.WriteLine("*** VarBind count: {0}", pkt.Pdu.VbList.Count);
                            Console.WriteLine("*** VarBind content:");
                            foreach (var v in pkt.Pdu.VbList)
                            {
                                Console.WriteLine(
                                    "**** {0} {1}: {2}",
                                    v.Oid,
                                    SnmpConstants.GetTypeName(v.Value.Type),
                                    v.Value);
                            }

                            Console.WriteLine("** End of SNMP Version 2 TRAP data.");
                        }
                    }
                }
                else
                {
                    if (inLength == 0)
                        Console.WriteLine("Zero length packet received.");
                }
            }
        }

        public void Dispose()
        {
            _trapReceiverTask?.Dispose();
        }
    }
}
