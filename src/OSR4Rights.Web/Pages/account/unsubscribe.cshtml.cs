using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages.Account
{
    public class UnsubscribeModel : PageModel
    {
        public string? Message { get; set; }

        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;
            var gmailPassword = AppConfiguration.LoadFromEnvironment().GmailPassword;

            // Notify an admin that a user has unsubscribed 
            var notifyEmail = new OSREmail(
                ToEmailAddress: "dave@hmsoftware.co.uk",
                Subject: "User unsubscribed",
                TextBody: $"Unsubscribe user {Email}",
                HtmlBody: $"Unsubscribe user {Email}"
            );

            var response = await Web.Email.Send(notifyEmail, postmarkServerToken, gmailPassword);

            return LocalRedirect("/account/unsubscribe-success");
        }
    }
}
