using System.Net.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace OSR4Rights.Web.Pages
{
    //[Authorize(Roles = "Tier1, Tier2, Admin")]
    public class FireMapModel : PageModel
    {
        public bool IsAllowedToUpload { get; set; }

        public IActionResult OnGet()
        {
            //var isAllowed = false;
            foreach (var claim in User.FindAll(ClaimTypes.Role))
            {
                // commented out for now - useful to forward if they have a proper claim
                // if there is any type of claim then they are logged in so can redirect
                //if (claim.Value == "Tier2")
                //    return LocalRedirect($"/fire-mapx");
                //if (claim.Value == "Admin")
                //    return LocalRedirect($"/fire-mapx");

                



                //if (claim.Value == "Tier2") isAllowed = true;
                //else if (claim.Value == "Admin") isAllowed = true;
            }
            //IsAllowedToUpload = isAllowed;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            foreach (var claim in User.FindAll(ClaimTypes.Role))
            {
                if (claim.Value == "Tier2")
                    return LocalRedirect($"/fire-mapx");
                if (claim.Value == "Admin")
                    return LocalRedirect($"/fire-mapx");
            }

            return Page();
        }

        // For user file uploads javascript handles the post

        // Sample data buttons
        //public IActionResult OnPostRun1Barack() =>
        //    LocalRedirect($"/face-search-go?createdFileName=1barackTIER1.zip");

        //public IActionResult OnPostRun6lfwsmall() =>
        //    LocalRedirect($"/face-search-go?createdFileName=6lfwsmallTIER1.zip");
    }
}
