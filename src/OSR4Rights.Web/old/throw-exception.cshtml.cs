using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages
{
    public class ExceptionModel : PageModel
    {

        public async Task<IActionResult> OnGetAsync(int jobId)
        {
            throw new ApplicationException("Some business logic has failed");
            return Page();
        }

    }
}
