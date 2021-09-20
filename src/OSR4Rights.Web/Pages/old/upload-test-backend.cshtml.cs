using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace OSR4Rights.Web.Pages
{
  
    public class UploadTestBackendModel : PageModel
    {
        //[BindProperty]
        //[Required(ErrorMessage = "Please select a file to upload")]
        //public IFormFile Upload { get; set; } = null!;
       

        public async Task OnGet()
        {
            //var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // Just want something to prove the db is responding
            //var result = await Db.GetCountOfAllVMs(connectionString);

        }
        
       

        //public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        //{
        //    // currently we just need some file to be uploaded
        //    if (ModelState.IsValid)
        //    {
        //        // check that Upload is something
        //        if (Upload is { } && Upload.Length > 0)
        //        {
        //            Log.Information($"Uploaded file {Upload.FileName}");

        //            // eg C:\Users\djhma\AppData\Local\Temp
        //            var path = Path.GetTempPath();

        //            var foo = DateTimeOffset.Now.ToUnixTimeSeconds();

        //            var fileName = Path.Combine(path, $"job-{foo}.tmp");

        //            // copy uploaded file to tempPath location as job-{jobId}.tmp
        //            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        //            {
        //                await Upload.CopyToAsync(fileStream, cancellationToken);
        //            }

        //            Log.Information($"Saved file {fileName}");

        //            return LocalRedirect($"/");
        //        }
        //    }

        //    return Page();
        //}
    }
}
