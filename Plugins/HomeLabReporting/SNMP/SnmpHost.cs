using GlobalLogger.AdvancedLogger;
using Newtonsoft.Json;
using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;

namespace HomeLabReporting.SNMP
{
    public class SnmpHostValueDefinition
    {
        public SnmpHostValueDefinition(string v)
        {
            Value = v;
        }

        public string Value { get; set; }
    }

    public class SnmpHostOidDefinition
    {
        public string Oid { get; set; }
        public string ReadableName { get; set; }
        public string Expression { get; set; }
        public string Format { get; set; } = "{0}";

        [JsonIgnore]
        public SnmpHostValueDefinition LastValue { get; set; }

        public string GetFormattedValue()
        {
            return double.TryParse(LastValue.Value, out var outResult) ? string.Format(Format, outResult) : string.Format(Format, LastValue.Value);
        }
    }

    public class SnmpHostMentionDefinition
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
    }

    public class SnmpHostTrapDefinition
    {
        public string Oid { get; set; }
        public string ReadableName { get; set; }
        public string Format { get; set; } = "{0}";
        public SnmpHostValueDefinition LastValue { get; set; } = null;
        public SnmpHostMentionDefinition MentionSettings { get; set; }

        public string GetFormattedValue()
        {
            return string.Format(Format, LastValue.Value);
        }
    }

    internal class SnmpHost
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string CommunityString { get; set; }
        public int ConnectionTimeout { get; set; }
        public bool ReceiveTraps { get; set; }
        public int PollInterval { get; set; }
        public DateTime LastContacted { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public int PollIntervalNext { get; set; }

        public List<SnmpHostOidDefinition> OidList = new List<SnmpHostOidDefinition>();
        public List<SnmpHostTrapDefinition> SnmpHostTraps = new List<SnmpHostTrapDefinition>();

        /// <summary>
        /// Executes the SNMP Queries needed to grab the Oid's data - usually this is
        /// </summary>
        public void Execute()
        {
            try
            {
                // SNMP community name
                var community = new OctetString(CommunityString);

                // Define agent parameters class
                var param = new AgentParameters(community) { Version = SnmpVersion.Ver1 };
                var agent = new IpAddress(IpAddress);
                var target = new UdpTarget((IPAddress)agent, Port, ConnectionTimeout, 1);

                var pdu = new Pdu(PduType.Get);

                foreach (var oidEntry in OidList.Where(x => !x.Oid.Equals(""))) //do not read empty OID's here
                    pdu.VbList.Add(oidEntry.Oid);

                var result = (SnmpV1Packet)target.Request(pdu, param);
                if (result == null) return;

                if (result.Pdu.ErrorStatus != 0)
                {
                    throw new Exceptions.ErrorInSnmpResponse(result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
                }

                // Go through all of the results we got back from SNMP, and match them against the oid entry in the main list.
                foreach (var resultEntry in result.Pdu.VbList)
                {
                    foreach (var oidEntry in OidList)
                    {
                        if (resultEntry.Oid.ToString() == oidEntry.Oid)
                        {
                            oidEntry.LastValue = new SnmpHostValueDefinition(resultEntry.Value.ToString());
                            //GlobalLogger.Logger.Instance.WriteConsole($"[SNMP Communication] - {Name} | OID {oidEntry.Oid} ({oidEntry.ReadableName}) updated with value {resultEntry.Value}");
                        }
                    }
                }

                var oidWithExpressions = OidList.Where(x => x.Expression != null);
                foreach (var oidWithExpression in oidWithExpressions)
                {
                    oidWithExpression.LastValue =
                        new SnmpHostValueDefinition(EvaluateExpression(oidWithExpression).ToString(CultureInfo.InvariantCulture));
                }

                LastContacted = DateTime.Now;
            }
            catch (Exception ex)
            {
                AdvancedLoggerHandler.Instance.GetLogger().Log($"Exception while attempting to compute SMTP Data:\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }

        private double EvaluateExpression(SnmpHostOidDefinition oidDefinition)
        {
            try
            {
                var tmpExpressionBuilder = OidList.Where(x => x.LastValue != null && x.Oid != "").Aggregate(oidDefinition.Expression, (current, oid) => current.Replace(oid.Oid, oid.LastValue.Value));

                var table = new DataTable();
                table.Columns.Add("expression", typeof(string), tmpExpressionBuilder);
                var row = table.NewRow();
                table.Rows.Add(row);

                var attemptedExpressionResult = double.Parse((string)row["expression"]);
                return attemptedExpressionResult;
            }
            catch (Exception ex)
            {
                return 0f;
            }
        }
    }
}