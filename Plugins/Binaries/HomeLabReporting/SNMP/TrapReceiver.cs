// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrapReceiver.cs" company="AGNGaming">
//   Copyright AGN Gaming -- Mark "AlienX" Phillips
// </copyright>
// <summary>
//   The trap receiver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HomeLabReporting.SNMP
{
    using SnmpSharpNet;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    internal class TrapReceiverConfiguration
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

        private TrapReceiverConfiguration _trapReceiverConfiguration;

        public delegate void EventRaiser(object sender, IpAddress ipAddress, VbCollection snmpVbCollection);

        public event EventRaiser OnTrapReceived;

        private const string ConfigurationPath = "Plugins\\Config\\Snmp.json";

        private readonly Thread _trapReceiverThread;

        private Socket socket;

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
                    GlobalLogger.Log4NetHandler.Log($"Unable to deserialize snmp.json", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception:ex);
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
                    GlobalLogger.Log4NetHandler.Log($"Exception while attempting to write to snmp.json", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception:ex);
                }
            }

            _trapReceiverThread = new Thread(new ThreadStart(StartTrapReceiver)) { Name = "SNMPTrapSocketListener" };
            _trapReceiverThread.Start();
        }

        /// <summary>
        /// The thing.
        /// </summary>
        private void StartTrapReceiver()
        {
            // Construct a socket and bind it to the trap manager port 162
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var ipep = new IPEndPoint(IPAddress.Any, 162);
            EndPoint ep = ipep;

            try
            {
                socket.Bind(ep);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            // Disable timeout processing. Just block until packet is received
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            while (socket.IsBound)
            {
                var inData = new byte[16 * 1024];

                // 16KB receive buffer int inlen = 0;
                var peer = new IPEndPoint(IPAddress.Any, 0);
                EndPoint inEndPoint = peer;
                var inLength = -1;
                try
                {
                    inLength = socket.ReceiveFrom(inData, ref inEndPoint);
                }
                catch (Exception ex)
                {
                    inLength = -1;
                }

                if (inLength > 0)
                {
                    // Check protocol version int
                    var ver = SnmpPacket.GetProtocolVersion(inData, inLength);
                    if (ver == (int)SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet
                        var pkt = new SnmpV1TrapPacket();
                        pkt.decode(inData, inLength);

                        OnTrapReceived?.Invoke(this, pkt.Pdu.AgentAddress, pkt.Pdu.VbList);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }

            _trapReceiverThread?.Abort();
        }
    }
}