using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class SpeechPartsModel : PageModel
    {
        public bool IsAllowedToUpload { get; set; }

        public void OnGet()
        {
            var isAllowed = false;
            foreach (var claim in User.FindAll(ClaimTypes.Role))
            {
                if (claim.Value == "Tier2") isAllowed = true;
                else if (claim.Value == "Admin") isAllowed = true;
            }

            IsAllowedToUpload = isAllowed;
        }

        // For user file uploads javascript handles the post

        // Sample data buttons
        // notice the TIER1 suffix on filenames

        public IActionResult OnPostRunFlac()
        {
            return LocalRedirect($"/speech-parts-go?createdFileName=test_vocal_aTIER1.flac");
        }

        //public IActionResult OnPostRunTESTfile() =>
        //    LocalRedirect($"/hate-speech-go?createdFileName=x2TESTfileTIER1.csv");

        //public IActionResult OnPostRunMultiLingualTest() =>
        //        LocalRedirect($"/hate-speech-go?createdFileName=x3multilingual-testTIER1.csv");

    }
}
