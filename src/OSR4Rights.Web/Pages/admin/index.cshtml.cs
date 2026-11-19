using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

namespace OSR4Rights.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public List<LoginAdminViewModel> Logins { get; set; } = null!;

        [BindProperty]
        public int SelectedTag { get; set; }
        public List<SelectListItem> Options { get; set; } = null!;

        public async Task OnGet()
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var loginStates = await Db.GetAllLoginStates(connectionString);

            // make collection with SelectListItem inside it
            var logins = await Db.GetAllLogins(connectionString);
            var loginAdminViewModels = new List<LoginAdminViewModel>();
            foreach (var x in logins)
            {
                string roleName = x.RoleId switch
                {
                    null => "none",
                    1 => "Tier1",
                    2 => "Tier2",
                    9 => "Admin",
                    _ => "unknown - problem"
                };

                string loginStateName = loginStates.First(a => a.LoginStateId == x.LoginStateId).Name.ToString();
                var foo2 = new LoginAdminViewModel(
                    LoginId: x.LoginId,
                    x.Email,
                    x.PasswordHash,
                    x.LoginStateId,
                    LoginStateName: loginStateName,
                    RoleName: roleName,
                    x.RoleId
                );
                loginAdminViewModels.Add(foo2);

            }
            Logins = loginAdminViewModels;
        }

        // Sample data buttons
        public async Task<IActionResult> OnPostSaveChangesAsync()
        {
            var foo = SelectedTag;

            Log.Information($"foo is {foo}");

            //return LocalRedirect($"/face-search-go?createdFileName=1barackTIER1.zip");
            return LocalRedirect("/admin");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var foo = SelectedTag;

            Log.Information($"foo is {foo}");
            //var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;
            //var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            //var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
            //    new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

            //var resourceGroupClient = resourcesClient.ResourceGroups;

            //// remove Rg's which look like
            //// webfacesearchgpu47

            //var foo = resourceGroupClient.List();

            //foreach (var bar in foo)
            //{
            //    if (bar.Name.StartsWith("webfacesearchgpu"))
            //    {
            //        await resourceGroupClient.StartDeleteAsync(bar.Name);
            //        Log.Information($"Manual Admin Delete of rg {bar.Name}");

            //        var vmId = await Db.GetVmIdByResourceGroupName(connectionString, bar.Name);

            //        Log.Information($"Manual Admin Update vmId {vmId} in our database to Deleted");
            //        await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);

            //        await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);
            //    }
            //}
            //return LocalRedirect("/admin");
            return Page();
        }
    }
}
