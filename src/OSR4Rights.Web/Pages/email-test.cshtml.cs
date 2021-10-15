using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    public class EmailTestModel : PageModel
    {
        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(Guid emailAddressConfirmationCode)
        {
            var textBody = $@"test - please disregard (text)";

            var htmlText = $@"<p>test - please disregard (html)</p> ";

            var osrEmail = new OSREmail(
                ToEmailAddress: Email,
                Subject: "Test Email - disregard",
                TextBody: textBody,
                HtmlBody: htmlText
            );

            var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

            var response = await Web.Email.Send(postmarkServerToken, osrEmail);

            if (response is null)
            {
                // Calls to the client can throw an exception 
                // if the request to the API times out.
                // or if the From address is not a Sender Signature 
                Log.Warning("Problem sending email");

                return Page();
            }

            if (response.Status != PostmarkStatus.Success)
            {
                Log.Warning($"Problem sending email - status: {response.Status}");
                return Page();
            }

            return Page();
        }
    }
}
