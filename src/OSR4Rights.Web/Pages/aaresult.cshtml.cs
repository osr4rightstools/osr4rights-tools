using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    //[Authorize(Roles = "Tier1, Tier2, Admin")]
    public class AAResultModel : PageModel
    {
        public AADto aadto { get; set; }

        //public async Task<IActionResult> OnPost(Guid aaguid)
        //{
           
        //}

        public async Task OnGet(Guid aaguid)
        {
            // query webservice to find out status
            var httpClient = new HttpClient();
            var url = $"http://hmsoftware.org/api/aa/{aaguid}";

            try
            {
                var data = new AADto { guid = aaguid };
                AADto? response = await httpClient.GetFromJsonAsync<AADto>(url);

                aadto = response;
                //AAText = "Processing";
                //AAGuid = foo.guid.ToString();

            }
            catch (Exception ex)
            {
                //AAText = "Sorry there was a problem - please try again later";
                Log.Error($"Problem with AA webservice {ex}");
            }
            //return Page();
            //var isAllowed = false;
            //foreach (var claim in User.FindAll(ClaimTypes.Role))
            //{
            //    if (claim.Value == "Tier2") isAllowed = true;
            //    else if (claim.Value == "Admin") isAllowed = true;
            //}

            //IsAllowedToUpload = isAllowed;
            //return Page();
        }

        public class AADto
        {
            public Guid? guid { get; set; }
            public string? url { get; set; }
            public string? cdn_url { get; set; }
            public string? screenshot { get; set; }
            public string? thumbnail { get; set; }
            public string status { get; set; }
        }

    }

}
