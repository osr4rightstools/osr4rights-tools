﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            if (User.Identity is { IsAuthenticated: true })
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // hopefully this would log them out seamlessly
                return LocalRedirect($"/account/email-address-confirmation/{emailAddressConfirmationCode}");
            }

            // We have a button on the page so that spam filters such as barracuda
            // which come and look at the email, and do requests on the page
            // get stopped
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid emailAddressConfirmationCode)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // get the guid and check whether it is a valid guid
            var login = await Db.GetLoginByEmailConfirmationCode(connectionString, emailAddressConfirmationCode);

            if (login is null)
            {
                Log.Warning($"Email address confirmation fail on guid {emailAddressConfirmationCode}");

                await Task.Delay(3000);
                return LocalRedirect("/account/email-address-confirmation-fail");

                //EmailConfirmationSuccess = false;

                //return Page();
            }

            EmailConfirmationSuccess = true;

            await Db.UpdateLoginIdWithLoginStateId(connectionString, login.LoginId, LoginStateId.InUse);

            // This only happens when user registers for the first time
            // so am happy to set RoleId to Tier1
            await Db.UpdateLoginIdWithRoleId(connectionString, login.LoginId, RoleId.Tier1);

            // Don't need the Confirmation Code Guid any more.
            await Db.UpdateLoginIdSetEmailAddressConfirmationCodeToNull(connectionString, login.LoginId);

            return LocalRedirect("/account/email-address-confirmation-success");
            //return Page();
        }
    }
}
