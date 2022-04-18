using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    public class IndexModel : PageModel
    {
        public string? HsResult { get; set; }

        public async Task OnGet(string? q)
        {
            if (q == null) { }
            else
            {
                Log.Information($"q is {q}");
                HsResult = "Contains hate";
            }
        }
    }
}
