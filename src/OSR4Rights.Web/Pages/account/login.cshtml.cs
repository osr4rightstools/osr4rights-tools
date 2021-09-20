using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [BindProperty]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [BindProperty] 
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }

        public IActionResult OnGetAsync(string? returnUrl = null)
        {
            // want to default the remember me checkbox to checked
            // and also remember state between login tries
            // ie if a user gets a login wrong and has unchecked remember me
            // then it stays unchecked
            //Input = new InputModel { RememberMe = true };

            // Has a logged in user somehow got back to the login screen without logging out first
            if (User.Identity is { IsAuthenticated: true })
            {
                return LocalRedirect("/");
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var login = await Db.GetLoginByEmail(connectionString, Email);

                if (login is null)
                {
                    Log.Information($"email address {Email} not found - but we wont tell the user that");
                    ModelState.AddModelError("Password", "Invalid login attempt");
                    return Page();
                }

                // Check hash
                var hashMatches = Password.HashMatches(login.PasswordHash);

                if (hashMatches == false)
                {
                    Log.Warning($"Password hash don't match for {Email}");

                    // increment numberOFailedLogins
                    await Db.IncrementNumberOfFailedLoginsForEmailLogin(connectionString, Email);

                    // lock the account if >= 3
                    var isLocked = await Db.CheckIfNeedToLockAccountForEmailLogin(connectionString, Email);

                    if (isLocked)
                    {
                        Log.Warning($"Login locked out {Email}");
                        ModelState.AddModelError("Password", "Account now locked out due to 3 wrong passwords - please contact us");
                    }
                    else
                        ModelState.AddModelError("Password", "Invalid login attempt");

                    return Page();
                }

                Log.Information($"Successful password hash match for email {Email} ");

                if (login.LoginStateId == LoginStateId.WaitingToBeInitiallyVerifiedByEmail)
                {
                    Log.Information($"Tried to login while waiting to be initally verified email confirmation {Email}");

                    ModelState.AddModelError("Password", "Waiting for email verification - please check your email or register again to resend the email");

                    return Page();
                }

                if (login.LoginStateId == LoginStateId.PasswordResetSent)
                {
                    Log.Information($"Tried to login while waiting for password reset to be sent {Email}");

                    ModelState.AddModelError("Password", "Waiting for password reset to be done - please check your email or do password reset again");

                    return Page();
                }

                if (login.LoginStateId == LoginStateId.LockedOutDueTo3WrongPasswords)
                {
                    Log.Information($"Locked out {Email}");

                    ModelState.AddModelError("Password", "Account locked out due to 3 wrong passwords - please contact us");

                    return Page();
                }

                // todo put in 2FA checks

                if (login.LoginStateId == LoginStateId.Disabled)
                {
                    Log.Warning($"User successful login but disabled {Email}");
                    ModelState.AddModelError("Password", "Disabled - please contact us to get the account reset");
                    return Page();
                }

                // Successful login so reset failed logins
                await Db.ResetFailedLoginsForEmailLogin(connectionString, Email);

                var cdRole = CDRole.Tier1;
                if (login.RoleId == 2)
                    cdRole = CDRole.Tier2;
                if (login.RoleId == 9)
                    cdRole = CDRole.Admin;

                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, Email),
                        new Claim(ClaimTypes.Role,  cdRole),
                        // custom claim type - so we can have our loginId available on every page via a Claim 
                        new Claim("LoginId", login.LoginId.ToString())
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                Log.Information($@"CDRole: {cdRole}");
                Log.Information($@"LoginId: {login.LoginId}");
                Log.Information($@"Remember me: {RememberMe}");

                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    IsPersistent = RememberMe, // false is default
                                               //IsPersistent = true,
                                               // Whether the authentication session is persisted across 
                                               // multiple requests. When used with cookies, controls
                                               // whether the cookie's lifetime is absolute (matching the
                                               // lifetime of the authentication ticket) or session-based.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // creates a 302 Found which then redirects to the resource
                return LocalRedirect(returnUrl ?? "/");
            }

            // Something failed. Redisplay the form.
            return Page();
        }
    }
}

