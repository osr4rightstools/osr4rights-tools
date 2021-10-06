using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSR4Rights.Web.Pages
{
    public class HostingEnvironmentModel : PageModel
    {
        public string? EnvironmentString { get; set; }

        public async Task OnGetAsync()
        {
            EnvironmentString = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }
    }
}
