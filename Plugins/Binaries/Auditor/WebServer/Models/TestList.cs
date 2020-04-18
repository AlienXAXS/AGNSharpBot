namespace Auditor.WebServer.Models
{
    internal class TestList
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Timestamp { get; set; }
        public long UserId { get; set; }
        public long ChannelId { get; set; }
        public string Contents { get; set; }
        public string PreviousContents { get; set; }
        public long MessageId { get; set; }
        public long GuildId { get; set; }
        public string Notes { get; set; }
        public string ImageUrls { get; set; }
    }
}