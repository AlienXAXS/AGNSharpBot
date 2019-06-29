using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using Auditor.WebServer.Models;
using Auditor.WebServer.Models.Messages;
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

                    var sktGuildId = (long) sktGuild.Id;
                    
                    var auditDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql.AuditEntry>();

                    var homepageModel = new Models.HomepageModel();
                    homepageModel.AuditedUsers = sktGuild.Users.Count;

                    homepageModel.AuditedDataRowCount = auditDb.Count();

                    homepageModel.DeletedMessagesSaved = auditDb.Count(entry =>
                        entry.Type == AuditorSql.AuditEntry.AuditType.MESSAGE_DELETED);

                    homepageModel.ImagesSaved = auditDb.Count(x => x.ImageUrls != "" && x.GuildId == sktGuildId);

                    homepageModel.OnlineUsers = sktGuild.Users.Count(x => x.Status != UserStatus.Offline);

                    homepageModel.OfflineUsers = sktGuild.Users.Count(x => x.Status == UserStatus.Offline);

                    homepageModel.RecordedMessages = auditDb.Count(entry => entry.Type == AuditorSql.AuditEntry.AuditType.MESSAGE_NEW && entry.GuildId == sktGuildId);

                    homepageModel.DatabaseFileSize = $"{new System.IO.FileInfo("Data\\Auditor.db").Length} Bytes";

                    return View["Index", homepageModel];
                }
                else
                    return Response.AsRedirect("/NoAuth");
            });

            Get("/NoAuth", args => View["NoAuth"]);

            Get("/login", args =>
            {
                // If we're already authed, do not do it again
                if (IsSessionAuthOk())
                    return Response.AsRedirect("/");

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
                    return Response.AsRedirect("/NoAuth");
                }
            });

            Get("/search/messages/by-user", args =>
            {
                if (IsSessionAuthOk())
                {
                    var sktGuild = GetDiscordGuildFromSession();
                    if (sktGuild == null)
                    {
                        DeauthSession();
                        return "Cannot find your discord guild id. You have been logged out!";
                    }

                    Models.Messages.ByUser model;
                    try
                    {
                        model = GenerateByUserModel(sktGuild);
                    }
                    catch (Exception ex)
                    {
                        model = new ByUser()
                        {
                            IsErrored = true,
                            ErrorMessage = ex.Message
                        };
                    }

                    return View["Messages_ByUser", model];
                }
                else
                    return Response.AsRedirect("/NoAuth");
            });

            Post("/search/messages/by-user", args =>
            {
                var sktGuild = GetDiscordGuildFromSession();
                if (sktGuild == null)
                {
                    DeauthSession();
                    return "Cannot find your discord guild id. You have been logged out!";
                }

                Models.Messages.ByUser model;
                try
                {
                    model = GenerateByUserModel(sktGuild);
                }
                catch (Exception ex)
                {
                    model = new ByUser()
                    {
                        IsErrored = true,
                        ErrorMessage =  ex.Message
                    };
                }

                return View["Messages_ByUser", model];
            });
        }

        private Models.Messages.ByUser GenerateByUserModel(SocketGuild sktGuild)
        {
            var model = new Models.Messages.ByUser();

            // Generate the drop down list of users
            var users = sktGuild.Users;
            foreach (var user in users.OrderBy(x => x.Username))
            {
                model.Users.Add(new Models.Messages.UserMakeup(user.Username, user.Nickname, user.Id));
            }

            // See if we have results
            if (Context.Request.Method.Equals("POST"))
            {
                var postData = this.Bind<Models.Messages.PostData>();

                foreach (var user in model.Users )
                {
                    if (user.Id.ToString() == postData.User)
                    {
                        user.MakeSelected();
                    }
                }

                DateTime.TryParse(postData.DatetimeRange_From, out var dtFrom);
                DateTime.TryParse(postData.DatetimeRange_To, out var dtTo);

                var auditDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection.Table<AuditorSql.AuditEntry>();

                long gId = (long) sktGuild.Id;

                if (!long.TryParse(postData.User, out var uId))
                    throw new Exception("Cannot convert user to Long, oopsie!");

                var foundAudits = auditDb.DefaultIfEmpty(null).Where(x => x != null && x.GuildId == gId && x.UserId == uId).ToList().OrderByDescending(x => x.Timestamp);

                Dictionary<string, SocketGuildUser> userMemory = new Dictionary<string, SocketGuildUser>();
                Dictionary<string, string> channelMemory = new Dictionary<string, string>();

                foreach (var audit in foundAudits)
                {
                    if (audit.Type != AuditorSql.AuditEntry.AuditType.MESSAGE_NEW &&
                        audit.Type != AuditorSql.AuditEntry.AuditType.MESSAGE_DELETED &&
                        audit.Type != AuditorSql.AuditEntry.AuditType.MESSAGE_MODIFIED) continue;

                    audit.Contents = HttpUtility.HtmlEncode(audit.Contents).Replace("@", "&#64;");
                    audit.Notes = HttpUtility.HtmlEncode(audit.Notes).Replace("@", "&#64;");

                    if ((postData.DatetimeRange_From != null && postData.DatetimeRange_To != null))
                        if ( !(audit.Timestamp.Ticks > dtFrom.Ticks && audit.Timestamp.Ticks < dtTo.Ticks) ) continue;

                    if (!channelMemory.ContainsKey(audit.ChannelId.ToString()))
                    {
                        var foundChannel = sktGuild.GetChannel((ulong) audit.ChannelId);
                        if (foundChannel == null)
                        {
                            if (audit.ChannelName == null)
                            {
                                audit.ChannelName = "Unknown Channel";
                                auditDb.Connection.Update(audit); //Update the record in the DB itself
                                channelMemory.Add(audit.ChannelId.ToString(), "Unknown Channel");
                            }
                        }
                        else
                        {
                            channelMemory.Add(audit.ChannelId.ToString(), foundChannel.Name);

                            if (audit.ChannelName == null)
                            {
                                audit.ChannelName = foundChannel.Name;
                                auditDb.Connection.Update(audit); //Update the record in the DB itself
                            }
                        }
                    }
                    else
                    {
                        if (audit.ChannelName == null)
                        {
                            audit.ChannelName = channelMemory[audit.ChannelId.ToString()];
                            auditDb.Connection.Update(audit); //Update the record in the DB itself
                        }
                        else
                        {
                            audit.ChannelName = channelMemory[audit.ChannelId.ToString()];
                        }
                    }

                    if (userMemory.ContainsKey(postData.User))
                    {
                        model.Results.Add(new SearchResults(audit, userMemory[postData.User]));
                    }
                    else
                    {
                        var sktUser = sktGuild.GetUser(Convert.ToUInt64(postData.User));
                        userMemory.Add(postData.User, sktUser);

                        model.Results.Add(new SearchResults(audit, sktUser));
                    }
                }
            }

            return model;
        }
    }
}
