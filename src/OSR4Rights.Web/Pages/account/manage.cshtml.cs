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
    }
}


