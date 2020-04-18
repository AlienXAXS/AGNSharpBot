using SQLite;
using System;

namespace Auditor
{
    public class AuditorSql
    {
        public class AuditEntry
        {
            public enum AuditType
            {
                USER_ONLINE,
                USER_OFFLINE,
                MESSAGE_NEW,
                MESSAGE_DELETED,
                MESSAGE_MODIFIED,
                JOIN_GUILD,
                QUIT_GUILD,
                IMAGE_UPLOAD,
                NICKNAME_NEW,
                NICKNAME_MODIFIED,
                NICKNAME_DELETED
            }

            [PrimaryKey, AutoIncrement]
            public long Id { get; set; }

            public AuditType Type { get; set; }
            public DateTime Timestamp { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; }
            public string Nickname { get; set; }
            public long ChannelId { get; set; }
            public string ChannelName { get; set; }
            public string Contents { get; set; }
            public string PreviousContents { get; set; }
            public long MessageId { get; set; }
            public long GuildId { get; set; }
            public string Notes { get; set; }
            public string ImageUrls { get; set; }
        }

        public class AuditorNancyLoginSession
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public long UserId { get; set; }
            public string AuthKey { get; set; }
            public long GuildId { get; set; }
        }
    }
}