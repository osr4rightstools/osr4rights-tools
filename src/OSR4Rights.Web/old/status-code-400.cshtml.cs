using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages
{
    public class StatusCode400 : PageModel
    {
        //public async Task OnGetAsync()
        public ActionResult OnGet()
        {
            return new StatusCodeResult(400);
        }
    }
}
