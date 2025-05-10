using GmailHandler.Models;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System.Collections.Generic;
using System.Linq;

namespace GmailHandler.Services.Implementation1
{
    public class UniqueSenderService : IUniqueSenderService
    {
        private readonly IAuthenticateService authenticateService;

        public UniqueSenderService(IAuthenticateService authenticateService)
        {
            this.authenticateService = authenticateService;
        }

        public List<UniqueSenderModel> GetUniqueSenders(int maxLength)
        {
            GmailService service = authenticateService.getServiceFromToken();
            var senderDict = new Dictionary<string, List<string>>();
            string pageToken = null; // Used for pagination
            int messagesRetrieved = 0; // Track how many messages have been retrieved

            // If maxLength is -1, fetch all emails without a specific limit
            if (maxLength == -1)
            {
                // Use pagination to fetch emails until all are retrieved
                do
                {
                    var request = service.Users.Messages.List("me");
                    request.MaxResults = 1000; // Fetch 1000 at a time
                    request.PageToken = pageToken;

                    var messageListResponse = request.Execute();
                    var messages = messageListResponse.Messages;

                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            var messageRequest = service.Users.Messages.Get("me", msg.Id);
                            messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                            messageRequest.MetadataHeaders = new List<string> { "From" };
                            var fullMessage = messageRequest.Execute();

                            string sender = fullMessage.Payload.Headers
                                .FirstOrDefault(h => h.Name == "From")?.Value ?? "Unknown";

                            if (!senderDict.ContainsKey(sender))
                                senderDict[sender] = new List<string>();

                            senderDict[sender].Add(msg.Id);
                        }
                    }

                    // Update the number of retrieved messages
                    messagesRetrieved += messages.Count;
                    pageToken = messageListResponse.NextPageToken; // Get the next page token

                } while (pageToken != null); // Continue until all messages are retrieved
            }
            else if (maxLength <= 1000)
            {
                // If maxLength <= 1000, just retrieve the specified number of messages (up to maxLength)
                var request = service.Users.Messages.List("me");
                request.MaxResults = maxLength; // Fetch up to maxLength messages
                var messageListResponse = request.Execute();
                var messages = messageListResponse.Messages;

                if (messages != null)
                {
                    foreach (var msg in messages)
                    {
                        var messageRequest = service.Users.Messages.Get("me", msg.Id);
                        messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                        messageRequest.MetadataHeaders = new List<string> { "From" };
                        var fullMessage = messageRequest.Execute();

                        string sender = fullMessage.Payload.Headers
                            .FirstOrDefault(h => h.Name == "From")?.Value ?? "Unknown";

                        if (!senderDict.ContainsKey(sender))
                            senderDict[sender] = new List<string>();

                        senderDict[sender].Add(msg.Id);
                    }
                }
            }
            else // maxLength > 1000
            {
                // If maxLength > 1000, use pagination and limit to maxLength
                do
                {
                    var request = service.Users.Messages.List("me");
                    request.MaxResults = 1000; // Fetch 1000 at a time
                    request.PageToken = pageToken;

                    var messageListResponse = request.Execute();
                    var messages = messageListResponse.Messages;

                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            var messageRequest = service.Users.Messages.Get("me", msg.Id);
                            messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                            messageRequest.MetadataHeaders = new List<string> { "From" };
                            var fullMessage = messageRequest.Execute();

                            string sender = fullMessage.Payload.Headers
                                .FirstOrDefault(h => h.Name == "From")?.Value ?? "Unknown";

                            if (!senderDict.ContainsKey(sender))
                                senderDict[sender] = new List<string>();

                            senderDict[sender].Add(msg.Id);
                        }

                        messagesRetrieved += messages.Count; // Track how many messages have been retrieved
                    }

                    pageToken = messageListResponse.NextPageToken; // Get the next page token

                } while (messagesRetrieved < maxLength && pageToken != null); // Continue until maxLength is reached or no more pages
            }

            // Return the grouped messages in a list of UniqueSenderModel
            return senderDict.Select(pair => new UniqueSenderModel
            {
                SenderName = pair.Key,
                MessageIds = pair.Value
            }).ToList();
        }

    }
}
