using GmailHandler.Services;
using Microsoft.AspNetCore.Mvc;

namespace GmailHandler.Controllers
{
    public class UniqueSendersController : Controller
    {
        private readonly IUniqueSenderService uniqueSenderService;

        public UniqueSendersController(IUniqueSenderService uniqueSenderService)
        {
            this.uniqueSenderService = uniqueSenderService;
        }

        [HttpGet]
        public IActionResult Index(string? maxLength)
        {
            // List of valid options for maxLength
            var validOptions = new List<int> { 10, 20, 30, 40, 50, 100, 200, 500, 1000, 1500, 2000, 2500 };

            // Default to 20 if maxLength is null or cannot be parsed
            int maxMessages = 20;

            // Try to parse maxLength if it's not null and is a valid number
            if (!string.IsNullOrEmpty(maxLength) && int.TryParse(maxLength, out int parsedMax))
            {
                // Check if the parsed maxLength exists in the valid options list
                if (validOptions.Contains(parsedMax))
                {
                    maxMessages = parsedMax;
                }
                else if (parsedMax == -1)
                {
                    maxMessages = -1;
                }
                else
                {
                    // If the maxLength is not valid, find the nearest smaller valid value
                    maxMessages = validOptions.Where(x => x <= parsedMax).Max();
                }
            }
            else
            {
                // If maxLength is not provided or not valid, default to 20
                maxMessages = 20;
            }

            // Fetch unique senders with the specified maxMessages
            var uniqueSenders = uniqueSenderService.GetUniqueSenders(maxMessages);

            // Set the maxLength value in ViewBag for the view to know which option was selected
            ViewBag.maxLength = maxMessages.ToString();  // Set as string for the dropdown

            return View(uniqueSenders);
        }


    }
}
