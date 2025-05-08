using System.Diagnostics;
using GmailHandler.Models;
using GmailHandler.Services;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.Mvc;

namespace GmailHandler.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAuthenticateService authService;


        public HomeController(
            ILogger<HomeController> logger, IAuthenticateService authService)
        {
            _logger = logger;
            this.authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            bool isAuthenticated=await authService.IsValidToken();
            if (!isAuthenticated)
            {
                return Redirect(authService.GenerateUrl());
            }

            return RedirectToAction("Index","Gmail");
        }
        [HttpGet("oauth2callback")]
        public async Task<IActionResult> oauth2callback(string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("No code found.");

            // Get tokens from the code
            await authService.ExchangeCodeForTokenAsync(code);


            return RedirectToAction("Index", "Gmail");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("log")]
        public IActionResult Logout()
        {
            authService.LogOut();

            // Redirect to Index to trigger re-authentication
            return RedirectToAction("Index");
        }

    }


}
