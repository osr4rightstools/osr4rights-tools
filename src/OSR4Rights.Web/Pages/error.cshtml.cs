using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-5.0
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public int? CustomStatusCode { get; set; }

        public void OnGet(int? statusCode = null)
        {
            var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            // a non 500 eg 404
            if (feature is { })
            {
                Log.Warning($"Http Status code {statusCode} on {feature.OriginalPath}");
                CustomStatusCode = statusCode;
                return;
            }

            // a 500
            // relying on serilog to output the error as we don't want the user to know

            // integration tests can call a page where the exceptionHandlerPathFeature can be null
            CustomStatusCode = 500;
        }

        //public ActionResult OnPost()
        public void OnPost(int? statusCode = null)
        {
            Log.Warning("An OnPost page threw an error. See error below. Maybe antiforgery");
            //Log.Warning("See Stacktrace in previous ERR log");
            //Log.Warning("ASP.NET failure - maybe antiforgery. Caught by OnPost Custom Error. Sending a 400 to the user which is probable");

            var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            // a non 500 eg 404
            if (feature is { })
            {
                Log.Warning($"Http Status code {statusCode} on {feature.OriginalPath}");
                CustomStatusCode = statusCode;
                return;
            }

            // a 500
            // relying on serilog to output the error as we don't want the user to know

            // integration tests can call a page where the exceptionHandlerPathFeature can be null
            CustomStatusCode = 500;
        }
    }
}
