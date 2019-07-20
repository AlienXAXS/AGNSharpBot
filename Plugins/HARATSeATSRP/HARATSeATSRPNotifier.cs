using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using Interface;
using Newtonsoft.Json;
using PluginManager;

namespace HARATSeATSRP
{
    [Export(typeof(IPlugin))]
    public class HARATSeATSRPNotifier : IPluginWithRouter
    {
        // this is all horribly hard-coded, but it doesn't matter

        public string Name => "HARATSeATSRPNotifier";
        public string Description => "Notifies HARAT Discord when new SRP requests have been logged into SeAT";

        public EventRouter EventRouter { get; set; }
        public PluginRouter PluginRouter { get; set; }

        private bool _seatWatcherRequestStop;
        private ulong _notificationChannelId = 328624738077507584;

        private List<SRPMemory> srpMemories;
        private const string srpMemoryFile = "Plugins\\Config\\SRPMemory.json";

        public void ExecutePlugin()
        {

            GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().OutputToConsole(true);

            try
            {
                var watcherThread = new Thread(StartSeatWatcher);

                if (System.IO.File.Exists(srpMemoryFile))
                {
                    srpMemories = JsonConvert.DeserializeObject<List<SRPMemory>>(System.IO.File.ReadAllText(srpMemoryFile));
                }
                else
                {
                    srpMemories = new List<SRPMemory>();
                }

                watcherThread.Start();
            }
            catch (Exception ex)
            {
                GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"Error in HARAT SRP Handler: {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        private async void StartSeatWatcher()
        {
            while (!_seatWatcherRequestStop)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var jsonFromSeAT = client.DownloadString("https://seat.housearatus.space/alienx/srpquery.php");
                        List<SRPTableItem> srpRequests =JsonConvert.DeserializeObject<List<SRPTableItem>>(jsonFromSeAT);

                        foreach (var guild in EventRouter.GetDiscordSocketClient().Guilds)
                        {
                            if ( !PluginRouter.IsPluginExecutableOnGuild(guild.Id) ) continue;

                            var notificationChannel = guild.Channels.DefaultIfEmpty(null).FirstOrDefault(x => x.Name.Equals("seat-srp-notifications"));

                            if ( notificationChannel != null )
                                foreach (var srpRequest in srpRequests)
                                {
                                    if (srpMemories.Any(x => x.KillToken.Equals(srpRequest.Kill_Token))) continue;

                                    var shipValue = $"{srpRequest.Cost:C} ISK".Replace("£", "");
                                    var builder = new EmbedBuilder();
                                    builder.Title = $"SRP Request From {srpRequest.Character_Name}";
                                    builder.AddField("Ship Type", srpRequest.Ship_Type);
                                    builder.AddField("ISK Value",shipValue);
                                    builder.AddField("Created At", srpRequest.Created_At);
                                    builder.AddField("zKillboard Link",$"https://zkillboard.com/kill/{srpRequest.Kill_Id}");

                                    if (notificationChannel is ISocketMessageChannel socketGuildChannel)
                                    {
                                        await socketGuildChannel.SendMessageAsync(embed: builder.Build());
                                    }

                                    srpMemories.Add(new SRPMemory(srpRequest.Kill_Token));
                                    System.IO.File.WriteAllText(srpMemoryFile, JsonConvert.SerializeObject(srpMemories, Formatting.Indented));
                                }
                        }
                    }
                } catch (Exception ex)
                {
                    Debug.Print("Lol");
                }
                finally
                {
                    Thread.Sleep(1000 * 60 * 5);
                }
            }
        }

        public void Dispose()
        {
            _seatWatcherRequestStop = true;
        }
    }

    public class SRPMemory
    {
        public string KillToken { get; set; }

        public SRPMemory(string killToken)
        {
            KillToken = killToken;
        }

    }

    public class SRPTableItem
    {
        public long User_Id { get; set; }
        public string Character_Name { get; set; }
        public long Kill_Id { get; set; }
        public string Kill_Token { get; set; }
        public byte Approved { get; set; }
        public double Cost { get; set; }
        public int Type_Id { get; set; }
        public string Ship_Type { get; set; }
        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
        public string Approver { get; set; }
    }
}
