using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using GlobalLogger;
using Pubg.Net;

namespace PUBGWeekly.Game
{
    class PubgWatcher
    {
        public static PubgWatcher Instance = _instance ?? (_instance = new PubgWatcher());
        private static readonly PubgWatcher _instance;

        public delegate void OnPubgGameEndedHandler(PubgWatcher instance, PubgMatch gameData);
        public event OnPubgGameEndedHandler OnPubgGameEnded;

        private readonly Thread _thread;
        private bool _threadRunning = true;

        public PubgWatcher()
        {
            PubgApiConfiguration.Configure(x => x.ApiKey = Configuration.JSON.PubgAPIConfigHandler.Instance.GetApiKey());
            _thread = new Thread(ThreadExecute) {IsBackground = true};
        }

        public void Start()
        {
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
                string gameJson = "";

                Dictionary<string, string> pubgMemory = new Dictionary<string, string>();

                while (_threadRunning)
                {
                    foreach (var pubgAccount in Configuration.PubgToDiscordManager.Instance.PubgAccountLinks)
                    {
                        var playerService = new PubgPlayerService();
                        var playerInfo = playerService.GetPlayer(PubgPlatform.Steam, pubgAccount.PubgAccountId);

                        var firstMatchId = playerInfo.MatchIds.FirstOrDefault();

                        if (pubgMemory.ContainsKey(pubgAccount.PubgAccountId))
                        {
                            var key = pubgMemory[pubgAccount.PubgAccountId];
                            if (key != firstMatchId)
                            {

                                var matchService = new PubgMatchService();
                                var matchData = matchService.GetMatch(firstMatchId);

                                OnPubgGameEnded?.Invoke(this, matchData);
                                _threadRunning = false;
                                break;
                            }
                        }
                        else
                        {
                            // Store the first known match id (the latest one the API returns), we can then scan that later to see if it's changed.
                            if (firstMatchId != null)
                                pubgMemory[pubgAccount.PubgAccountId] = firstMatchId;
                        }

                        Thread.Sleep(1000 * 15);
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log($"[PubgWeekly-ThreadExecute] Exception when handing pubg api", Log4NetHandler.LogLevel.ERROR, exception:ex);
            }
        }
    }
}
