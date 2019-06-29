using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Auditor.WebServer.Models.Messages
{

    class UserMakeup
    {
        public string Name { get; private set; }
        public string Nickname { get; private set; }
        public bool HasNickname { get; private set; }
        public long Id { get; private set; }
        public string Username { get; private set; }

        public string InnerHtml { get; private set; }

        public UserMakeup(string name, string nickname, ulong id)
        {
            Name = name;
            Nickname = nickname;
            HasNickname = Nickname != null;
            Id = (long) id;
            Username = Nickname != null ? $"{Name} ({Nickname})" : Name;
            InnerHtml = $"<option value=\"{id}\">{Username}</option>";
        }

        public void MakeSelected()
        {
            InnerHtml = $"<option selected=\"selected\" value=\"{Id}\">{Username}</option>";
        }
    }

    class SearchResults
    {

        public string Username { get; set; } //The username in format: USERNAME (NICKNAME)
        public string Message { get; set; } // The actual message
        public string EntryType { get; set; } // Deleted, Modified, New
        public string EntryTime { get; set; } // The time the message was saved
        public string Notes { get; set; } // Notes of the message, can be useful if the guild member has since left
        public string ChannelName { get; set; }

        public SearchResults(AuditorSql.AuditEntry audit, SocketGuildUser sktGuildUser)
        {
            Username = sktGuildUser == null ? "Unknown User" :
                sktGuildUser.Nickname == null ? sktGuildUser.Username :
                $"{sktGuildUser.Username} ({sktGuildUser.Nickname})";

            if (audit.Type == AuditorSql.AuditEntry.AuditType.MESSAGE_MODIFIED)
                Message = $"{audit.PreviousContents} -> {audit.Contents}";
            else
                Message = audit.Contents;

            switch (audit.Type)
            {
                case AuditorSql.AuditEntry.AuditType.MESSAGE_NEW:
                    EntryType = "New";
                    break;

                case AuditorSql.AuditEntry.AuditType.MESSAGE_MODIFIED:
                    EntryType = "Modified";
                    break;

                case AuditorSql.AuditEntry.AuditType.MESSAGE_DELETED:
                    EntryType = "Deleted";
                    break;
            }

            EntryTime = audit.Timestamp.ToString(CultureInfo.CurrentCulture);

            Notes = audit.Notes;

            ChannelName = audit.ChannelName;
        }
    }

    class ByUser
    {
        public bool IsErrored { get; set; }
        public string ErrorMessage { get; set; }
        public List<UserMakeup> Users = new List<UserMakeup>();
        public List<SearchResults> Results = new List<SearchResults>();
    }
}
