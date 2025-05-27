using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreMvcTemplate.Data;
using AspNetCoreMvcTemplate.Resources;
using Microsoft.Extensions.Localization;
using System.Globalization;
using AspNetCoreMvcTemplate.Models;
using Microsoft.AspNetCore.Localization;

namespace AspNetCoreMvcTemplate.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _userManager = userManager;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            ViewBag.WelcomeMessage = _localizer["Home.Welcome"];
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Also set our custom cookie for the custom provider
            Response.Cookies.Append(
                "Language",
                culture,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
