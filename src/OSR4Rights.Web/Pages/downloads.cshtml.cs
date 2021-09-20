using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    // User has to be logged in (authenticated) and have an assigned Role (they get Tier1 after clicked on their email address confirm email)
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class DownloadsModel : PageModel
    {
        private readonly IHostEnvironment _hostEnvironment;

        public DownloadsModel(IHostEnvironment hostEnvironment) => _hostEnvironment = hostEnvironment;

        public async Task<IActionResult> OnGet(int jobId, string fileName)
        {
            // /downloads directory is not in wwwroot so the static file serving can never hit it
            //var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "downloads", jobId.ToString(), fileName);

            var filePath = Path.Combine("/mnt/osrshare/", "downloads", jobId.ToString(), fileName);

            // Is this Login allowed to look at this jobId?
            var loginId = Helper.GetLoginIdAsInt(HttpContext);

            // Does the requested file exist on filesystem?
            var exists = System.IO.File.Exists(filePath);
            if (!exists)
            {
                Log.Warning($"{filePath} requested which was not found by loginId {loginId}. Application problem?");
                return NotFound();
            }

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;
            var isAllowed = await Db.CheckIfLoginIdIsAllowedToViewThisJobId(connectionString, loginId, jobId);

            if (!isAllowed)
            {
                Log.Warning($"loginId {loginId} who is authorised and has a role, requested jobId {jobId} filename {fileName} which they don't have access to");
                // todo it would be nice to preserve the url which is denied.
                return LocalRedirect("/account/access-denied");
            }

            //Log.Information($"Successful download request for jobId: {jobId} filename: {fileName} by loginId {loginId}");

            // Get the content type which could be html, csv, zip
            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(fileName, out string contentType)) contentType = "application/octet-stream";

            // why is this returning a 304?
            return PhysicalFile(filePath, contentType);
        }
    }
}
