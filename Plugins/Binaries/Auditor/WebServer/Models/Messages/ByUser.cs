using System.Collections.Generic;
using System.Globalization;
using Discord.WebSocket;

namespace Auditor.WebServer.Models.Messages
{
    internal class UserMakeup
    {
        public UserMakeup(string name, string nickname, ulong id)
        {
            Name = name;
            Nickname = nickname;
            HasNickname = Nickname != null;
            Id = (long) id;
            Username = Nickname != null ? $"{Name} ({Nickname})" : Name;
            InnerHtml = $"<option value=\"{id}\">{Username}</option>";
        }

        public string Name { get; }
        public string Nickname { get; }
        public bool HasNickname { get; }
        public long Id { get; }
        public string Username { get; }

        public string InnerHtml { get; private set; }

        public void MakeSelected()
        {
            InnerHtml = $"<option selected=\"selected\" value=\"{Id}\">{Username}</option>";
        }
    }

    internal class SearchResults
    {
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

        public string Username { get; set; } //The username in format: USERNAME (NICKNAME)
        public string Message { get; set; } // The actual message
        public string EntryType { get; set; } // Deleted, Modified, New
        public string EntryTime { get; set; } // The time the message was saved
        public string Notes { get; set; } // Notes of the message, can be useful if the guild member has since left
        public string ChannelName { get; set; }
    }

    internal class ByUser
    {
        public List<SearchResults> Results = new List<SearchResults>();
        public List<UserMakeup> Users = new List<UserMakeup>();
        public bool IsErrored { get; set; }
        public string ErrorMessage { get; set; }
    }
}