using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    [Authorize]
    public class ManageLoginModel : PageModel
    {
        public IHttpContextAccessor HttpContextAccessor { get; }

        public string Email { get; set; } = null!;

        public string RoleLevel { get; set; } = null!;

        public ManageLoginModel(IHttpContextAccessor httpContextAccessor) => HttpContextAccessor = httpContextAccessor;

        public void OnGet()
        {
            Email = User.Identity!.Name!;

            var roleClaims = User.FindAll(ClaimTypes.Role);

            // Login will probably only have 1 Claim ie Tier1, Tier2, Admin
            foreach (var claim in roleClaims)
            {
                RoleLevel += claim.Value + " ";
            }
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;
                var login = await Db.GetLoginByEmail(connectionString, Email);

                if (login == null)
                {
                    Log.Information("Forgot-password email not found in our db was entered, but we won't tell the user that");
                    return LocalRedirect("/account/forgot-password-confirmation");
                }

                var guid = Guid.NewGuid();

                await Db.UpdateLoginIdForgotPasswordResetWithTimeAndGuid(connectionString, (int)login.LoginId, guid);

                // send email
                // todo refactor out this helper
                var request = HttpContextAccessor.HttpContext?.Request;

                // eg https
                var scheme = request?.Scheme;

                // eg localhost:5001
                var host = request?.Host.ToUriComponent();

                var foo = $"{scheme}://" + host + $"/account/reset-password/{guid}";
                Log.Information(foo);

                var textBody = $@"Hi,
Here is your OSR4Rights Tools email reset link: {foo}
Please click this link within 1 hour from now
";

                var htmlText = $@"<p>Hi,</p>
<p>Here is your OSR4Rights Tools email reset link:</p>
<p><a href=""{foo}"">{foo}</a></p>
<p>Please click this link within 1 hour from now</p>
                    ";

                var osrEmail = new OSREmail(
                    ToEmailAddress: Email,
                    Subject: "OSR4RightsTools Password Reset",
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
                    ModelState.AddModelError("Password", "Sorry problem sending the confirmation email");
                    return Page();
                }

                if (response.Status != PostmarkStatus.Success)
                {
                    ModelState.AddModelError("Password", $"Problem sending email - status: {response.Status}");
                    return Page();
                }


                return LocalRedirect("/account/forgot-password-confirmation");
            }

            return Page();
        }

    }
}


