namespace Auditor.WebServer.Models
{
    internal class HomepageModel
    {
        public int OnlineUsers { get; set; }
        public int OfflineUsers { get; set; }
        public long RecordedMessages { get; set; }
        public long DeletedMessagesSaved { get; set; }
        public long ImagesSaved { get; set; }
        public long AuditedUsers { get; set; }
        public long AuditedDataRowCount { get; set; }
        public string DatabaseFileSize { get; set; }
    }
}