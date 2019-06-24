using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Auditor.WebServer.Models;
using Nancy;
using Nancy.Helpers;

namespace Auditor.WebServer
{
    public class TestModule : NancyModule
    {
        public TestModule()
        {
            Get("/", args =>
            {



                return View["Index", this.Request.Url];
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
