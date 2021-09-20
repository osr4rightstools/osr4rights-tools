using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

namespace OSR4Rights.Web.Pages.Admin.Logins
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        public LoginSmall LoginSmall { get; set; } = null!;

        [BindProperty]
        public int SelectedLoginStateId { get; set; }
        public List<SelectListItem> LoginStateOptions { get; set; } = null!;


        [BindProperty]
        public int? SelectedRoleId { get; set; }
        public List<SelectListItem> RoleOptions { get; set; } = null!;

        public bool ViewingOwnLoginId { get; set; }

        public async Task OnGet(int loginId)
        {
            Log.Information($"LoginId is {loginId}");
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var loginSmall = await Db.GetLoginByLoginId(connectionString, loginId);
            LoginSmall = loginSmall;

            var loginStates = await Db.GetAllLoginStates(connectionString);

            LoginStateOptions = loginStates.Select(x =>
                new SelectListItem
                {
                    Value = x.LoginStateId.ToString(),
                    Text = x.Name
                }).ToList();
            SelectedLoginStateId = loginSmall.LoginStateId;


            // I've got the Db table waiting there
            RoleOptions = new List<SelectListItem>()
            {
                new("none", null),
                new("Tier1", 1.ToString()),
                new("Tier2", 2.ToString()),
                new("Admin", 9.ToString())
            };

            SelectedRoleId = loginSmall.RoleId;

            var loginIdClaim = Helper.GetLoginIdAsInt(HttpContext);
            if (loginIdClaim == loginId) ViewingOwnLoginId = true;
        }

        public async Task<IActionResult> OnPostAsync(int loginId, string? returnUrl = null)
        {
            var loginIdToUpdate = loginId;

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // don't let user change the state of own login
            var loginIdClaim = Helper.GetLoginIdAsInt(HttpContext);

            if (loginIdToUpdate == loginIdClaim)
            {
                Log.Warning($"Login {loginId} tried to update their own login");
                return LocalRedirect($"/admin/logins/edit/{loginId}");
            }

            await Db.UpdateLoginStateIdAndRoleIdByLoginId(connectionString, loginIdToUpdate, SelectedLoginStateId, SelectedRoleId);


           //return LocalRedirect($"/admin/logins/edit/{loginId}");
            return LocalRedirect($"/admin");
        }
    }
}
