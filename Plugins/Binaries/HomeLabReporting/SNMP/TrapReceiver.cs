// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrapReceiver.cs" company="AGNGaming">
//   Copyright AGN Gaming -- Mark "AlienX" Phillips
// </copyright>
// <summary>
//   The trap receiver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GlobalLogger;
using Newtonsoft.Json;
using SnmpSharpNet;

namespace HomeLabReporting.SNMP
{
    internal class TrapReceiverConfiguration
    {
        public int Port { get; set; } = 162;
    }

    /// <summary>
    ///     The trap receiver.
    /// </summary>
    public class TrapReceiver : IDisposable
    {
        public delegate void EventRaiser(object sender, IpAddress ipAddress, VbCollection snmpVbCollection);

        private const string ConfigurationPath = "Plugins/Config/Snmp.json";
        private static readonly TrapReceiver _instance;
        public static TrapReceiver Instance = _instance ?? (_instance = new TrapReceiver());

        private readonly Thread _trapReceiverThread;

        private TrapReceiverConfiguration _trapReceiverConfiguration;

        private Socket socket;

        public TrapReceiver()
        {
            if (File.Exists(ConfigurationPath))
                try
                {
                    _trapReceiverConfiguration =
                        JsonConvert.DeserializeObject<TrapReceiverConfiguration>(
                            File.ReadAllText(ConfigurationPath));
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log("Unable to deserialize snmp.json", Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
            else
                try
                {
                    File.WriteAllText(ConfigurationPath, JsonConvert.SerializeObject(
                        new TrapReceiverConfiguration(),
                        Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log("Exception while attempting to write to snmp.json",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }

            _trapReceiverThread = new Thread(StartTrapReceiver) {Name = "SNMPTrapSocketListener"};
            _trapReceiverThread.Start();
        }

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }

            // This should abort itself when the socket above is killed.
            //_trapReceiverThread?.Abort();
        }

        public event EventRaiser OnTrapReceived;

        /// <summary>
        ///     The thing.
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
                    
                    // Catch the thread being disposed here
                    if (socket == null)
                        return;
                }
                catch (Exception ex)
                {
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

                        OnTrapReceived?.Invoke(this, pkt.Pdu.AgentAddress, pkt.Pdu.VbList);
                    }
                }
            }
        }
    }
}