using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            // php vm uses this to logout

            //Log.Warning("Should not hit this logout page directly. Should use a post ");
            //return LocalRedirect("/error");
            Log.Information($"User {User?.Identity?.Name} logged out from PHP Side");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Does a 302Found GET request to the current page
            //return RedirectToPage();
            return RedirectToPage("/account/logout-success");
        }

        public async Task<IActionResult> OnPost()
        {
            Log.Information($"User {User?.Identity?.Name} logged out");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Does a 302Found GET request to the current page
            //return RedirectToPage();
            return RedirectToPage("/account/logout-success");
        }
    }
}


