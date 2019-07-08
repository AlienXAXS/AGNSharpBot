﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GlobalLogger.AdvancedLogger;
using PluginManager;
using Responses.SQLTables;

namespace Responses.Informational
{
    class LastOnlineDbHandler
    {

        public Task StartOnlineScanner(EventRouter discordSocketClient)
        {
            discordSocketClient.GuildMemberUpdated += delegate (SocketGuildUser oldUser, SocketGuildUser NewUser)
            {
                try
                {
                    // Is bot - dont care
                    if (NewUser.IsBot) return Task.CompletedTask;

                    // If the old user and the new user have the same status, we also dont care
                    if (oldUser.Status.Equals(NewUser.Status)) return Task.CompletedTask;

                    var sqlDb = InternalDatabase.Handler.Instance.GetConnection().DbConnection
                        .Table<SQLTables.LastOnlineTable>();

                    var foundUser = sqlDb.DefaultIfEmpty(null)
                        .FirstOrDefault(x => x != null && x.DiscordId.Equals((long) NewUser.Id)) ?? new LastOnlineTable();
                    var isInsert = foundUser.DiscordId.Equals(0);

                    if (NewUser.Status == UserStatus.Offline)
                    {
                        foundUser.DiscordId = (long) NewUser.Id;
                        foundUser.DateTime = DateTime.Now;

                        if (isInsert)
                            sqlDb.Connection.Insert(foundUser);
                        else
                            sqlDb.Connection.Update(foundUser);
                    }
                    else
                    {
                        // If it is not a new insert, we just delete the users entry from the table, this assumes they are online
                        if (!isInsert)
                            sqlDb.Connection.Delete(foundUser);
                    }
                }
                catch (Exception ex)
                {
                    AdvancedLoggerHandler.Instance.GetLogger().Log($"Exception while attempting to handle GuildMemberUpdated for OnlineScanner:\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
                }

                return Task.CompletedTask;
            };
            return Task.CompletedTask;
        }
    }
}
