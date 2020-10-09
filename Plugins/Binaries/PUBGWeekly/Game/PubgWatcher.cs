using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GlobalLogger;
using Pubg.Net;
using PUBGWeekly.Configuration;
using PUBGWeekly.Configuration.JSON;

namespace PUBGWeekly.Game
{
    internal class PubgWatcher
    {
        public delegate void OnPubgGameEndedHandler(PubgWatcher instance, PubgMatch gameData);

        public static PubgWatcher Instance = _instance ?? (_instance = new PubgWatcher());
        private static readonly PubgWatcher _instance;

        private Thread _thread;
        private bool _threadRunning = true;

        public PubgWatcher()
        {
            PubgApiConfiguration.Configure(x => x.ApiKey = PubgAPIConfigHandler.Instance.GetApiKey());
        }

        public event OnPubgGameEndedHandler OnPubgGameEnded;

        public void Start()
        {
            _thread = new Thread(ThreadExecute) {IsBackground = true};
            _threadRunning = true;
            _thread.Start();
        }

        public void Stop()
        {
            _threadRunning = false;
        }

        private void ThreadExecute()
        {
            try
            {
                var gameJson = "";

                var pubgMemory = new Dictionary<string, string>();

                while (_threadRunning)
                {
                    foreach (var pubgAccount in PubgToDiscordManager.Instance.PubgAccountLinks)
                    {
                        var playerService = new PubgPlayerService();
                        var playerInfo = playerService.GetPlayer(PubgPlatform.Steam, pubgAccount.PubgAccountId);

                        var firstMatchId = playerInfo.MatchIds.FirstOrDefault();

                        if (pubgMemory.ContainsKey(pubgAccount.PubgAccountId))
                        {
                            var key = pubgMemory[pubgAccount.PubgAccountId];
                            Console.WriteLine($"###### Current Key {key}, new key: {firstMatchId}");
                            if (key != firstMatchId)
                            {
                                var matchService = new PubgMatchService();
                                var matchData = matchService.GetMatch(firstMatchId);

                                Console.WriteLine("#### INVOKING!");
                                OnPubgGameEnded?.Invoke(this, matchData);
                                _threadRunning = false;
                                break;
                            }
                        }
                        else
                        {
                            // Store the first known match id (the latest one the API returns), we can then scan that later to see if it's changed.
                            if (firstMatchId != null)
                            {
                                pubgMemory[pubgAccount.PubgAccountId] = firstMatchId;
                                Console.WriteLine($"###### Found match ID of: {firstMatchId}");
                            }
                        }

                        Thread.Sleep(1000 * 15);
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("[PubgWeekly-ThreadExecute] Exception when handing pubg api",
                    Log4NetHandler.LogLevel.ERROR, exception: ex);
            }
        }
    }
}