using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using VoiceChannelRoles.SQLite.Tables;

namespace VoiceChannelRoles.SQLite
{
    internal static class SQLiteHandler
    {
        public static void Add(ulong guildId, ulong vcId, ulong roleId)
        {
            var dbConnection = InternalDatabase.Handler.Instance.GetConnection();
            if (dbConnection == null) throw new Exception("No Database");

            dbConnection.DbConnection.Insert(new Channels()
            {
                GuildId = (long) guildId,
                VoiceChannelId = (long) vcId,
                RoleId = (long) roleId
            });
        }

        public static bool Check(ulong guildId, ulong vcId)
        {
            var dbConnection = InternalDatabase.Handler.Instance.GetConnection();
            if (dbConnection == null) throw new Exception("No Database");

            return dbConnection.DbConnection.Table<Channels>().Any(x => x != null && x.GuildId.Equals((long)guildId) && x.VoiceChannelId.Equals((long)vcId));
        }

        public static void Remove(ulong guildId, ulong vcId)
        {
            var dbConnection = InternalDatabase.Handler.Instance.GetConnection();
            if ( dbConnection == null ) return;

            var foundEntry = dbConnection.DbConnection.Table<Channels>().DefaultIfEmpty(null).FirstOrDefault(x =>
                x != null && x.GuildId.Equals((long) guildId) && x.VoiceChannelId.Equals((long) vcId));

            if (foundEntry != null)
            {
                dbConnection.DbConnection.Delete(foundEntry);
            }
        }

        public static ulong GetRoleId(ulong guildId, ulong vcId)
        {
            var dbConnection = InternalDatabase.Handler.Instance.GetConnection();
            if (dbConnection == null) throw new Exception("No Database");

            var dbEntry = dbConnection.DbConnection.Table<Channels>().DefaultIfEmpty(null).FirstOrDefault(x =>
                x != null && x.GuildId.Equals((long) guildId) && x.VoiceChannelId.Equals((long) vcId));

            if (dbEntry != null)
            {
                return (ulong) dbEntry.RoleId;
            }
            else
            {
                throw new Exception("No DB Entry");
            }
        }
    }
}
