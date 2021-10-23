using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        public IHttpContextAccessor HttpContextAccessor { get; }

        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public ForgotPasswordModel(IHttpContextAccessor httpContextAccessor) => HttpContextAccessor = httpContextAccessor;

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;
                var login = await Db.GetLoginByEmail(connectionString, Email);

                if (login == null)
                {
                    Log.Information($"Forgot-password email {Email} not found in our db was entered, but we won't tell the user that");
                    return LocalRedirect("/account/forgot-password-confirmation");
                }

                var guid = Guid.NewGuid();

                await Db.UpdateLoginIdForgotPasswordResetWithTimeAndGuid(connectionString, (int)login.LoginId, guid);

                // send email
                // todo refactor out this helper
                var request = HttpContextAccessor.HttpContext?.Request;

                // eg https
                //var scheme = request?.Scheme;
                // force as we are using reverse proxy communicating over http

                // old email way
//                var scheme = "https";

//                // eg localhost:5001
//                var host = request?.Host.ToUriComponent();

//                var foo = $"{scheme}://" + host + $"/account/reset-password/{guid}";
//                Log.Information(foo);

//                var textBody = $@"Hi,
//Here is your OSR4Rights Tools email reset link: {foo}
//Please click this link within 1 hour from now
//";

//                var htmlText = $@"<p>Hi,</p>
//<p>Here is your OSR4Rights Tools email reset link:</p>
//<p><a href=""{foo}"">{foo}</a></p>
//<p>Please click this link within 1 hour from now</p>
//                    ";

//                var osrEmail = new OSREmail(
//                    //ToEmailAddress: Email,
//                    ToEmailAddress: login.Email,
//                    Subject: "OSR4RightsTools Password Reset",
//                    TextBody: textBody,
//                    HtmlBody: htmlText
//                );

                var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;
                //var gmailPassword = AppConfiguration.LoadFromEnvironment().GmailPassword;

                //var response = await Web.Email.Send(osrEmail, postmarkServerToken, gmailPassword);
                var response = await Web.Email.SendTemplate("forgot-password", Email, guid.ToString(), postmarkServerToken);

                if (response == false)
                {
                    ModelState.AddModelError("Password", "Sorry problem sending the forgot password email");
                    return Page();
                }

                return LocalRedirect("/account/forgot-password-confirmation");
            }

            return Page();
        }
    }
}


