using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using GmailHandler.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GmailHandler.Services.Implementation1
{
    public class GmailServiceWrapper : IGmailServiceWrapper
    {
        
        private readonly IAuthenticateService _authService;
        private readonly IMemoryCache _cache;

        public GmailServiceWrapper(IAuthenticateService authenticateService, IMemoryCache cache)
        {
           
            _authService = authenticateService;
            _cache = cache;
        }
        public async Task<string> GetUserEmailAsync()
        {
            GmailService _gmailService = _authService.getServiceFromToken();

            // This calls Gmail API’s users.getProfile endpoint
            var profile = await _gmailService.Users.GetProfile("me").ExecuteAsync();
            return profile.EmailAddress;
        }
        public async Task<(List<EmailSummary> Emails, string NextToken)> GetEmailsByPageTokenAsync(string pageToken, int pageSize)
        {
            GmailService _gmailService = _authService.getServiceFromToken();

            var request = _gmailService.Users.Messages.List("me");
            request.MaxResults = pageSize;
            request.PageToken = pageToken;
            Console.WriteLine("page token: " + pageToken);
            var response = await request.ExecuteAsync();
            var emails = new List<EmailSummary>();

            foreach (var message in response.Messages ?? new List<Message>())
            {
                var fullMessage = await _gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
                emails.Add(new EmailSummary
                {
                    Id = fullMessage.Id,
                    From = fullMessage.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value,
                    Subject = fullMessage.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value,
                    Snippet = fullMessage.Snippet
                });
            }

            return (emails, response.NextPageToken);
        }
        public async Task<Message> GetMessageByIdAsync(string messageId)
        {
            var userEmail = await GetUserEmailAsync();
            if (string.IsNullOrEmpty(userEmail))
                throw new Exception("User email not found or user not authenticated.");

            var gmailService = _authService.getServiceFromToken();// assuming this returns the authorized GmailService
            var request = gmailService.Users.Messages.Get(userEmail, messageId);
            return await request.ExecuteAsync();
        }

        public  string GetMessageBody(Google.Apis.Gmail.v1.Data.Message message)
        {
            if (message.Payload?.Body?.Data != null)
                return DecodeBase64Url(message.Payload.Body.Data);

            return GetBodyFromParts(message.Payload?.Parts);
        }

        private  string GetBodyFromParts(IList<MessagePart> parts)
        {
            if (parts == null) return string.Empty;

            foreach (var part in parts)
            {
                if (part.MimeType == "text/plain" || part.MimeType == "text/html")
                {
                    if (!string.IsNullOrEmpty(part.Body?.Data))
                        return DecodeBase64Url(part.Body.Data);
                }

                // Check nested parts
                var nested = GetBodyFromParts(part.Parts);
                if (!string.IsNullOrEmpty(nested))
                    return nested;
            }

            return string.Empty;
        }

        private  string DecodeBase64Url(string input)
        {
            input = input.Replace("-", "+").Replace("_", "/");
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }
            var bytes = Convert.FromBase64String(input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private List<string> ParseMessageIds(string messageIds)
        {
            if (string.IsNullOrWhiteSpace(messageIds))
                return new List<string>();

            return messageIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .ToList();
        }

        public void DeleteMessages(List<string> messageIds)
        {
            if (messageIds == null || messageIds.Count == 0)
                return;
            List<string> modifiedMessageIds = messageIds
            .SelectMany(message => ParseMessageIds(message))
            .ToList();
            GmailService _gmailService = _authService.getServiceFromToken();
            Console.WriteLine("message ids " + messageIds.ToString());
            // Use BatchDelete request
            var batchDeleteRequest = new Google.Apis.Gmail.v1.Data.BatchDeleteMessagesRequest
            {
                Ids = modifiedMessageIds
            };

            _gmailService.Users.Messages.BatchDelete(batchDeleteRequest, "me").Execute();
        }

    }
}
