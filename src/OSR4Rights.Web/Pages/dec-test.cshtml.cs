using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MimeKit;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    public class DecTestModel : PageModel
    {
        [BindProperty] [EmailAddress] public string Email { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            // ONE - grab the CookieAuthenticationOptions instance
            var opt = HttpContext.RequestServices
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme); //or use .Get("Cookies")

            // TWO - Get the encrypted cookie value
            var cookie = opt.CookieManager.GetRequestCookie(HttpContext, opt.Cookie.Name);

            // THREE - decrypt it
            var asdf = opt.TicketDataFormat.Unprotect(cookie);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

            var response = await Web.Email.SendTemplate("register", Email, Guid.NewGuid().ToString(), postmarkServerToken);

            if (response == false)
            {
                Log.Warning("Problem sending email");

                return Page();
            }

            //var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;
            //var gmailPassword = AppConfiguration.LoadFromEnvironment().GmailPassword;

            //var textBody = $@"test - please disregard (text)";

            //var htmlText = "";

            //var osrEmail = new OSREmail(
            //    ToEmailAddress: Email,
            //    Subject: "Please confirm email address for registration with OSR4RightsTools",
            //    TextBody: textBody,
            //    HtmlBody: htmlText
            //);

            //var response = await Web.Email.Send(osrEmail, postmarkServerToken, gmailPassword);


            //if (response == false)
            //{
            //    Log.Warning("Problem sending email");

            //    return Page();
            //}





            //var mailMessage = new MimeMessage();
            //mailMessage.From.Add(new MailboxAddress("Dave Mateer", "davemateer@gmail.com"));
            //mailMessage.To.Add(new MailboxAddress("Dave Work Email ", "dave@hmsoftware.co.uk"));
            //mailMessage.Subject = "test email";
            //mailMessage.Body = new TextPart("plain")
            //{
            //    Text = "Hello, test email"
            //};

            //using (var smtpClient = new SmtpClient())
            //{
            //    smtpClient.Connect("smtp.gmail.com", 587, true);
            //    smtpClient.Authenticate("user", "password");
            //    smtpClient.Send(mailMessage);
            //    smtpClient.Disconnect(true);
            //}

            return Page();
        }

        // Postmark
        //public async Task<IActionResult> OnPostAsync()
        //{
        //    var textBody = $@"test - please disregard (text)";

        //    var htmlText = $@"<p>test - please disregard (html)</p> ";

        //    var osrEmail = new OSREmail(
        //        ToEmailAddress: Email,
        //        Subject: "Test Email - disregard",
        //        TextBody: textBody,
        //        HtmlBody: htmlText
        //    );

        //    var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

        //    var response = await Web.Email.Send(postmarkServerToken, osrEmail);

        //    if (response is null)
        //    {
        //        // Calls to the client can throw an exception 
        //        // if the request to the API times out.
        //        // or if the From address is not a Sender Signature 
        //        Log.Warning("Problem sending email");

        //        return Page();
        //    }

        //    if (response.Status != PostmarkStatus.Success)
        //    {
        //        Log.Warning($"Problem sending email - status: {response.Status}");
        //        return Page();
        //    }

        //    return Page();
        //}
    }
}
