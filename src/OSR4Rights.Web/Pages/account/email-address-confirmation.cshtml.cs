using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class EmailAddressConfirmationModel : PageModel
    {
        public string? Message { get; set; }

        public bool EmailConfirmationSuccess { get; set; }

        public async Task<IActionResult> OnGet(Guid emailAddressConfirmationCode)
        {
            // Has a logged in user somehow got to this page?
            if (User.Identity is { IsAuthenticated: true }) return LocalRedirect("/logout");

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // get the guid and check whether it is a valid guid
            var login = await Db.GetLoginByEmailConfirmationCode(connectionString, emailAddressConfirmationCode);

            if (login is null)
            {
                Log.Warning($"Email address confirmation fail on guid {emailAddressConfirmationCode}");

                await Task.Delay(3000);

                EmailConfirmationSuccess = false;

                return Page();
            }

            EmailConfirmationSuccess = true;

            await Db.UpdateLoginIdWithLoginStateId(connectionString, login.LoginId, LoginStateId.InUse);

            // This only happens when user registers for the first time
            // so am happy to set RoleId to Tier1
            await Db.UpdateLoginIdWithRoleId(connectionString, login.LoginId, RoleId.Tier1);
            
            // Don't need the Confirmation Code Guid any more.
            await Db.UpdateLoginIdSetEmailAddressConfirmationCodeToNull(connectionString, login.LoginId);

            return Page();
        }
    }
}
