using GmailHandler.Models;
using GmailHandler.Services;
using GmailHandler.Services.Implementation1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace GmailHandler.Controllers
{
    public class GmailController : Controller
    {
        private readonly IGmailServiceWrapper _gmailServiceWrapper;
        private readonly IMemoryCache _cache;
        private const int PageSize = 10;
        private readonly IAiService aiService;

        public GmailController(IGmailServiceWrapper gmailServiceWrapper, IMemoryCache cache, IAiService aiService)
        {
            _gmailServiceWrapper = gmailServiceWrapper;
            _cache = cache;
            this.aiService = aiService;
        }

        public async Task<IActionResult> Index(int page = 0)
        {
            var userEmail = await _gmailServiceWrapper.GetUserEmailAsync();
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not authenticated or email is not available.");
            }

            var tokenKey = $"pageToken_{userEmail}_{page}";
            string pageToken = null;

            // Get the cached token for this user and page
            if (page > 0 && !_cache.TryGetValue(tokenKey, out pageToken))
            {
                // Fall back to last available page for this user
                int lastKnownPage = page - 1;
                while (lastKnownPage >= 0)
                {
                    if (_cache.TryGetValue($"pageToken_{userEmail}_{lastKnownPage}", out string knownToken))
                    {
                        return RedirectToAction("Index", new { page = lastKnownPage });
                    }
                    lastKnownPage--;
                }
                return RedirectToAction("Index", new { page = 0 });
            }

            var (emails, nextToken) = await _gmailServiceWrapper.GetEmailsByPageTokenAsync(pageToken, PageSize);

            if (!string.IsNullOrEmpty(nextToken))
            {
                _cache.Set($"pageToken_{userEmail}_{page + 1}", nextToken, TimeSpan.FromMinutes(10));
            }

            ViewBag.Page = page;
            ViewBag.HasNext = !string.IsNullOrEmpty(nextToken);
            ViewBag.HasPrevious = page > 0;

            return View(emails);
        }

        [HttpGet("clearcache")]
        public async Task<IActionResult> ClearCache()
        {
            var userEmail = await _gmailServiceWrapper.GetUserEmailAsync();

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not authenticated or email is not available.");
            }

            // This will only work if we can iterate through keys, which MemoryCache doesn't expose directly.
            // So we do a full clear instead, OR if using a more advanced caching lib, you could scope per user.
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Clear all (or you can implement per-user cache registry for selective clear)
            }

            return RedirectToAction("Index", new { page = 0 });
        }

        [HttpGet("message/details/{messageId}")]
        public async Task<IActionResult> MessageDetails(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
                return BadRequest("Message ID is required.");

            try
            {
                var message = await _gmailServiceWrapper.GetMessageByIdAsync(messageId);
                Console.WriteLine(message);
                var body = _gmailServiceWrapper.GetMessageBody(message);
                ViewBag.Body = body;

                return View(message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving message: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult DeleteGmail(List<string> messageIds)
        {
            if (messageIds != null && messageIds.Any())
            {
                _gmailServiceWrapper.DeleteMessages(messageIds);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("message/summary/{messageId}")]
        public async Task<IActionResult> Messagesummary(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
                return BadRequest("Message ID is required.");

            try
            {
                var message = await _gmailServiceWrapper.GetMessageByIdAsync(messageId);
                Console.WriteLine(message);
                var body = _gmailServiceWrapper.GetMessageBody(message);
                var headers = message.Payload.Headers;
                string subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value;
                string from = headers.FirstOrDefault(h => h.Name == "From")?.Value;
                string addedText = $"This email's subject is {subject} and from {from} and body is ";
                string aiSummary = aiService.summary(addedText+body);
                ViewBag.Body = body;
                ViewBag.Summary = aiSummary;
                return View(message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving message: {ex.Message}");
            }
        }



    }
}
