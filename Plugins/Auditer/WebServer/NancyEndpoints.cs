using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Auditor.WebServer.Models;
using Discord;
using Discord.WebSocket;
using Nancy;
using Nancy.Helpers;
using Nancy.ModelBinding;

namespace Auditor.WebServer
{
    public sealed class NancyEndpoints : NancyModule
    {
        private bool IsSessionAuthOk()
        {
            var authKey = Request.Session["authKey"]?.ToString();
            if (authKey == null) return false;

            var authDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                .Table<AuditorSql.AuditorNancyLoginSession>();
            var foundAuthToken = authDb.FirstOrDefault(x => x.AuthKey == authKey);
            return foundAuthToken != null;
        }

        private SocketGuild GetDiscordGuildFromSession()
        {
            var strGuildId = Request.Session["DiscordGuildId"]?.ToString();
            if (strGuildId == null) return null;

            if (strGuildId == null)
                return null;

            if (ulong.TryParse(strGuildId, out var discordGuildId))
            {
                return NancyServer.Instance.DiscordSocketClient.GetGuild(discordGuildId);
            }
            else
                return null;
        }

        private void DeauthSession()
        {
            Request.Session["authKey"] = null;
            Request.Session["DiscordGuildId"] = null;
        }

        public NancyEndpoints()
        {
            Get("/", args =>
            {
                if (IsSessionAuthOk())
                {
                    // Create our model

                    var sktGuild = GetDiscordGuildFromSession();
                    if (sktGuild == null)
                    {
                        DeauthSession();
                        return "Cannot find your discord guild id. You have been logged out!";
                    }

                    var auditDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                        .Table<AuditorSql.AuditEntry>();

                    var homepageModel = new Models.HomepageModel
                    {
                        AuditedUsers = sktGuild.Users.Count,
                        AuditedDataRowCount = auditDb.Count(),
                        DeletedMessagesSaved = auditDb.Count(entry =>
                            entry.Type == AuditorSql.AuditEntry.AuditType.MESSAGE_DELETED),
                        ImagesSaved = auditDb.Count(x => x.ImageUrls != "" && x.GuildId == (long)sktGuild.Id),
                        OnlineUsers = sktGuild.Users.Count(x => x.Status != UserStatus.Offline),
                        OfflineUsers = sktGuild.Users.Count(x => x.Status == UserStatus.Offline),
                        RecordedMessages = auditDb.Count(entry => entry.Type == AuditorSql.AuditEntry.AuditType.MESSAGE_NEW && entry.GuildId == (long)sktGuild.Id),
                        DatabaseFileSize = $"{new System.IO.FileInfo("Data\\Auditor.db").Length} Bytes"
                    };

                    return View["Index", homepageModel];
                }
                else
                    return View["NoAuth", this.Request.Url];
            });

            Get("/login", args =>
            {
                // If we're already authed, do not do it again
                if (IsSessionAuthOk())
                {
                    return Response.AsRedirect("/");
                }

                var request = this.Bind<RequestObjects.LoginRequest>();

                if ( request.Key == null )
                    return View["NoAuth", this.Request.Url];

                var authDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql.AuditorNancyLoginSession>();

                var foundAuthToken = authDb.FirstOrDefault(x => x.AuthKey == request.Key);
                if (foundAuthToken != null)
                {
                    Request.Session["authKey"] = request.Key;
                    Request.Session["DiscordGuildId"] = foundAuthToken.GuildId.ToString();
                    return Response.AsRedirect("/");
                }
                else
                {
                    return View["NoAuth", this.Request.Url];
                }
            });

            Get("/test", args =>
            {
                List<Models.TestList> users = new List<TestList>();

                var dbEntries = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql.AuditEntry>()
                    .ToList();

                foreach (var dbEntry in dbEntries)
                {
                    users.Add(new TestList()
                    {
                        ChannelId = dbEntry.ChannelId,
                        Contents = dbEntry.Contents != null ? HttpUtility.HtmlEncode(dbEntry.Contents).Replace("@", "&#64;") : "",
                        GuildId = dbEntry.GuildId,
                        Id = dbEntry.Id,
                        ImageUrls = dbEntry.ImageUrls,
                        Type = dbEntry.Type.ToString(),
                        PreviousContents = HttpUtility.HtmlEncode(dbEntry.PreviousContents),
                        Timestamp = HttpUtility.HtmlEncode(dbEntry.Timestamp.ToString()),
                        UserId = dbEntry.UserId,
                        Notes = dbEntry.Notes,
                        MessageId = dbEntry.MessageId
                    });
                }

                return View["testpage", users];
            });
        }
    }
}
