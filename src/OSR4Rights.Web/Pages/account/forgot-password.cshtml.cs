using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

                var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

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


