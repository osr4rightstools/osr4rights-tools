using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        [BindProperty]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Must be at least {2} characters, and have at least 1 capital letter", MinimumLength = 8)]
        public string NewPassword { get; set; } = null!;


        public async Task<IActionResult> OnGet()
        {
            // Has a logged in user somehow got to this reset-password page?
            // this can happen if they want to change their password
            // so log them out
            if (User.Identity is { IsAuthenticated: true })
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Page();
            }

            // we don't check the guid is valid to stop people guessing guids
            return Page();
        }


        public async Task<IActionResult> OnPostAsync(Guid resetGuid)
        {
            if (NewPassword.Any(char.IsUpper) != true)
                ModelState.AddModelError("NewPassword", "At least 1 capital letter");


            if (ModelState.IsValid)
            {
                var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

                // get loginId that we want to do an update on their password
                var login = await Db.GetLoginByPasswordResetVerificationCode(connectionString, resetGuid);

                if (login == null)
                {
                    Log.Warning($"Failed password reset on an unrecognised resetGuid {resetGuid} DateTimeUtc combination");
                    ModelState.AddModelError("NewPassword", "There has been a problem resetting your password - try sending a new reset email");
                    await Task.Delay(3000);
                    return Page();
                }

                if (login.LoginStateId == LoginStateId.Disabled)
                {
                    Log.Warning($"Failed password reset on an account which is disabled {login.Email}");
                    ModelState.AddModelError("NewPassword", "Account is disabled - please contact us");
                    await Task.Delay(3000);
                    return Page();
                }

                if (login.LoginStateId == LoginStateId.InUse)
                {
                    throw new ApplicationException($"Trying to reset a password on an account {login.Email} which is in use. Should never happen");
                }

                var newPasswordHash = NewPassword!.HashPassword();

                await Db.UpdateLoginPasswordAndResetFailedLoginsAndVerificationCode(connectionString, (int)login.LoginId, newPasswordHash);

                Log.Information($"Success password reset for {login.Email}");

                return LocalRedirect("/account/reset-password-confirmation");
            }

            // ModelState invalid redisplay the form.
            return Page();
        }
    }
}
