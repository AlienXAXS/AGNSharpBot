using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Discord;
using Discord.WebSocket;
using GlobalLogger;
using Interface;
using Newtonsoft.Json;
using PluginManager;

namespace HARATSeATSRP
{
    [Export(typeof(IPlugin))]
    public class HARATSeATSRPNotifier : IPluginWithRouter
    {
        private const string srpMemoryFile = "Plugins\\Config\\SRPMemory.json";
        private ulong _notificationChannelId = 328624738077507584;

        private bool _seatWatcherRequestStop;

        private List<SRPMemory> srpMemories;

        private Thread watcherThread;
        // this is all horribly hard-coded, but it doesn't matter

        public string Name => "HARATSeATSRPNotifier";
        public string Description => "Notifies HARAT Discord when new SRP requests have been logged into SeAT";

        public EventRouter EventRouter { get; set; }
        public PluginRouter PluginRouter { get; set; }

        public void ExecutePlugin()
        {
            try
            {
                watcherThread = new Thread(StartSeatWatcher) {IsBackground = true};

                if (File.Exists(srpMemoryFile))
                    srpMemories = JsonConvert.DeserializeObject<List<SRPMemory>>(File.ReadAllText(srpMemoryFile));
                else
                    srpMemories = new List<SRPMemory>();

                watcherThread.Start();
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("Error in HARAT SRP Log4NetHandler", Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }

        public void Dispose()
        {
            _seatWatcherRequestStop = true;
            watcherThread?.Interrupt();
        }

        private async void StartSeatWatcher()
        {
            while (!_seatWatcherRequestStop)
                try
                {
                    using (var client = new WebClient())
                    {
                        var jsonFromSeAT = client.DownloadString("https://seat.housearatus.space/alienx/srpquery.php");
                        if (jsonFromSeAT == "{}") continue;

                        var srpRequests = JsonConvert.DeserializeObject<List<SRPTableItem>>(jsonFromSeAT);

                        foreach (var guild in EventRouter.GetDiscordSocketClient().Guilds)
                        {
                            if (!PluginRouter.IsPluginExecutableOnGuild(guild.Id)) continue;

                            var notificationChannel = guild.Channels.DefaultIfEmpty(null)
                                .FirstOrDefault(x => x.Name.Equals("seat-srp-notifications"));

                            if (notificationChannel != null)
                                foreach (var srpRequest in srpRequests)
                                {
                                    if (srpMemories.Any(x => x.KillToken.Equals(srpRequest.Kill_Token))) continue;

                                    var shipValue = $"{srpRequest.Cost:C} ISK".Replace("£", "");
                                    var builder = new EmbedBuilder();
                                    builder.Title = $"SRP Request From {srpRequest.Character_Name}";
                                    builder.AddField("Ship Type", srpRequest.Ship_Type);
                                    builder.AddField("ISK Value", shipValue);
                                    builder.AddField("Created At", srpRequest.Created_At);
                                    builder.AddField("zKillboard Link",
                                        $"https://zkillboard.com/kill/{srpRequest.Kill_Id}");

                                    if (notificationChannel is ISocketMessageChannel socketGuildChannel)
                                        await socketGuildChannel.SendMessageAsync(embed: builder.Build());

                                    srpMemories.Add(new SRPMemory(srpRequest.Kill_Token));
                                    File.WriteAllText(srpMemoryFile,
                                        JsonConvert.SerializeObject(srpMemories, Formatting.Indented));
                                }
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log("Unable to download the SeAT JSON file.", Log4NetHandler.LogLevel.ERROR,
                        exception: ex);
                }
                finally
                {
                    //Sleep no matter what, even if we did error we should still sleep so we dont spam the server
                    Thread.Sleep(1000 * 60 * 5);
                }
        }
    }

    public class SRPMemory
    {
        public SRPMemory(string killToken)
        {
            KillToken = killToken;
        }

        public string KillToken { get; set; }
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