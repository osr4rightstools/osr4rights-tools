using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSR4Rights.Web.Pages
{
    public class HealthCheckDbModel : PageModel
    {
        public int CountOfAllVMs { get; set; }


        public async Task OnGet()
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // Just want something to prove the db is responding
            var result = await Db.GetCountOfAllVMs(connectionString);
            CountOfAllVMs = result;

        }
    }
}
