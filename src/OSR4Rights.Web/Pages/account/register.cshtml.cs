using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        public IHttpContextAccessor HttpContextAccessor { get; }

        // don't know why jquery validate isn't firing
        [BindProperty]
        [EmailAddress]
        public string EmailB { get; set; } = null!;

        [BindProperty]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Must be at least {2} characters long, and 1 capital letter", MinimumLength = 8)]
        public string PasswordB { get; set; } = null!;

        // simple captcha
        [BindProperty]
        public string Answer { get; set; }

        // honeypot field which is nullable
        [BindProperty]
        public string Email2 { get; set; }


        public RegisterModel(IHttpContextAccessor httpContextAccessor) => HttpContextAccessor = httpContextAccessor;

        public IActionResult OnGet()
        {
            // Has a logged in user somehow got to this Register page?
            if (User.Identity is { IsAuthenticated: true }) return LocalRedirect("/");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ModelState.Remove("Email2");
            if (Email2 == null)
            {
                Log.Information("honeypot field not filled out - good!");
            }
            else
            {
                Log.Warning("STAGE1 bot protect on /account/register - honeypot field has something in it - possible bot");
                // am failing out, so lets write to the logfile for interest
                Log.Warning($"  email {EmailB} ");
                Log.Warning($"  password {PasswordB} ");
                Log.Warning($"  email2 {Email2} ");
                ModelState.AddModelError("Password", "Are you a human?");
            }

            ModelState.Remove("Answer");
            if (Answer.ToLower() == "edinburgh")
            {
                Log.Information(Answer);
                Log.Information("Captcha correctly answered as edinburgh.. see above for what was typed");
            }
            else
            {
                Log.Warning("STAGE2 bot protect on /account/registrer - Captcha not correct!");

                Log.Warning($"  captcha {Answer} ");
                Log.Warning($"  email {EmailB} ");
                Log.Warning($"  password {PasswordB} ");
                Log.Warning($"  email2 {Email2} ");

                ModelState.AddModelError("Answer", "Try again.. are you sure you are human?");
            }

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            //ReturnUrl = returnUrl;
            if (PasswordB.Any(char.IsUpper) != true)
                ModelState.AddModelError("Password", "At least 1 capital letter");

            if (ModelState.IsValid)
            {
                // Check if the email exists in db?
                var result = await Db.GetLoginByEmail(connectionString, EmailB);

                if (result is { })
                {
                    if (result.LoginStateId == LoginStateId.WaitingToBeInitiallyVerifiedByEmail)
                    {
                        // User is registering again with an unverified email - this is fine.
                        await Db.DeleteLoginWithEmail(connectionString, EmailB);
                    }
                    else
                    {
                        Log.Information($@"User tried to register an already registered email address {EmailB}");
                        ModelState.AddModelError("Email", "Sorry email address is already registered - try logging in or resetting password");
                        return Page();
                    }
                }

                // Insert new login in the database
                // we will allow upper and lower cases in email addresses
                // https://webmasters.stackexchange.com/questions/34056/is-it-ok-to-use-uppercase-letters-in-an-email-address/34058
                // but will not allow duplicate accounts with UpperAndLower
                var login = new LoginSmall
                (
                    // Will be assigned by the Db
                    LoginId: null,
                    EmailB,
                    PasswordB.HashPassword(),
                    LoginStateId.WaitingToBeInitiallyVerifiedByEmail,
                    // RoleId is null until LoginStateId past WaitingToBeInitiallyVerifiedByEmail ie 2 - InUse
                    // which is set in /account/email-address-confirmation
                    RoleId: null
                );

                Log.Information($@"New user successfully registered: {EmailB}");
                var returnedLogin = await Db.InsertLogin(connectionString, login);

                // Generate EmailAddressConfirmationCode which will be checked by /account/email-address-confirmation
                var guid = Guid.NewGuid();
                await Db.UpdateLoginEmailAddressConfirmationCode(connectionString, returnedLogin.LoginId, guid);

                // shortcut way to get the url eg https://localhost500 or https://testserver.azure.com
                // to make testing easier
                var request = HttpContextAccessor.HttpContext?.Request;

                // eg https
                // we are using a reverse proxy, which is communicating over http
                // so this will always give http
                //var scheme = request?.Scheme;

                // we are forcing redirect to https on nginx, so can safely specify http here
                var scheme = "https";

                // eg localhost:5001
                var host = request?.Host.ToUriComponent();

                // old email stuff before template
                //                var foo = $"{scheme}://" + host + $"/account/email-address-confirmation/{guid}";
                //                Log.Information(foo);

                //                var textBody = $@"Hi,
                //Here is your OSR4Rights Tools email address confirmation link: {foo}
                //Please click this link within 1 hour from now
                //Or register again if you miss this time
                //";

                //                var htmlText = $@"<p>Hi,</p>
                //<p>Here is your OSR4Rights Tools email address confirmation link: </p>
                //<p><a href=""{foo}"">{foo}</a></p>
                //<p>Please click this link within 1 hour from now</p>
                //<p>Or register again if you miss this time</p>
                //                    ";

                //                var osrEmail = new OSREmail(
                //                    ToEmailAddress: Email,
                //                    Subject: "OSR4RightsTools Account Confirm",
                //                    TextBody: textBody,
                //                    HtmlBody: htmlText
                //                );

                var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

                //var response = await Web.Email.Send(osrEmail, postmarkServerToken, gmailPassword);
                var response = await Web.Email.SendTemplate("register", EmailB, guid.ToString(), postmarkServerToken);

                if (response == false)
                {
                    // Calls to the client can throw an exception 
                    // if the request to the API times out.
                    // or if the From address is not a Sender Signature 
                    ModelState.AddModelError("Email", "Sorry problem sending the confirmation email - please try again later. We are working on resolving it.");
                    return Page();
                }

                // Notify an admin that a new user has signed up
                var notifyEmail = new OSREmail(
                    ToEmailAddress: "dave@hmsoftware.co.uk",
                    Subject: "New User Registered",
                    TextBody: $"New User Registered on OSR {EmailB}",
                    HtmlBody: $"New User Registered on OSR {EmailB}"
                );

                var gmailPassword = AppConfiguration.LoadFromEnvironment().GmailPassword;
                var notifyEmailResponse = await Web.Email.Send(notifyEmail, postmarkServerToken, gmailPassword);

                return LocalRedirect("/account/register-success");
            }

            // Something failed. Redisplay the form.
            return Page();
        }
    }
}
