using GmailHandler.Models;
using Google.Apis.Gmail.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GmailHandler.Services
{
    public interface IGmailServiceWrapper
    {
        Task<string> GetUserEmailAsync();

        Task<(List<EmailSummary> Emails, string NextToken)> GetEmailsByPageTokenAsync(string pageToken, int pageSize);

        public Task<Message> GetMessageByIdAsync(string messageId);

        public string GetMessageBody(Google.Apis.Gmail.v1.Data.Message message);

        public void DeleteMessages(List<string> messageIds);
    }
}
