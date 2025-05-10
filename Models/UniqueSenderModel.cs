namespace GmailHandler.Models
{
    public class UniqueSenderModel
    {
        public string SenderName { get; set; }
        public List<string> MessageIds { get; set; } = new();
        public int MessageCount => MessageIds.Count;
    }
}
