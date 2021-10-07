using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        public List<Dashboard500VM> Dashboard500Vms { get; set; } = null!;
        public List<DashboardRealPage> DashboardRealPages { get; set; } = null!;

        public async Task OnGet()
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var dashboard500VMs = await Db.GetDashboard500VMs(connectionString);
            Dashboard500Vms = dashboard500VMs;

            var dashboardRealPages = await Db.GetDashboardRealPages(connectionString);
            DashboardRealPages = dashboardRealPages;

            //Options = loginStates.Select(x =>
            //    new SelectListItem
            //    {
            //        Value = x.LoginStateId.ToString(),
            //        Text = x.Name
            //    }).ToList();
            //SelectedTag = 2;

            // make collection with SelectListItem inside it
            //var logins = await Db.GetAllLogins(connectionString);
            //var loginAdminViewModels = new List<LoginAdminViewModel>();
            //foreach (var x in logins)
            //{
            //    string roleName = x.RoleId switch
            //    {
            //        null => "none",
            //        1 => "Tier1",
            //        2 => "Tier2",
            //        9 => "Admin",
            //        _ => "unknown - problem"
            //    };

            //    string loginStateName = loginStates.First(a => a.LoginStateId == x.LoginStateId).Name.ToString();
            //    var foo2 = new LoginAdminViewModel(
            //        LoginId: x.LoginId,
            //        x.Email,
            //        x.PasswordHash,
            //        x.LoginStateId,
            //        LoginStateName: loginStateName, 
            //        RoleName: roleName,
            //        x.RoleId
            //    );
            //    loginAdminViewModels.Add(foo2);

            //}
            //Logins = loginAdminViewModels;


            //var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            //var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
            //    new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

            //var resourceGroupClient = resourcesClient.ResourceGroups;

            //// how to get the resourcegroupname which look like
            //// webfacesearchgpu47
            //var foo = resourceGroupClient.List();
            //foreach (var bar in foo)
            //{
            //    if (bar.Name.StartsWith("webfacesearchgpu")) FaceSearchRgs.Add(bar.Name);
            //}


            //foreach (var bar in foo)
            //{
            //    if (bar.Name.StartsWith("webhatespeechcpu")) HateSpeechRgs.Add(bar.Name);
            //}
        }

    }
}
