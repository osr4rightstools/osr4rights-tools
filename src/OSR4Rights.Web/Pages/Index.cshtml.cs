using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Azure.ResourceManager.Network.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    public class IndexModel : PageModel
    {
        public string? HSText { get; set; }
        public string? HSScore { get; set; }
        public string? HSPrediction { get; set; }

        public string? AAText { get; set; }
        public string? AAGuid { get; set; }

        public string? CacheBust { get; set; }

        public async Task<IActionResult> OnGet(string? route, string? q)
        {
            var base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            CacheBust = base64Guid;

            if (route == "hate")
            {
                Log.Information($"route is hate");
                q ??= "This is really not hate speech, even though I said hate";

                // call webservice
                // http://hmsoftware.org/hs
                // POST 
                // { "text":"dave is an awesome dude!" }

                var httpClient = new HttpClient();
                var url = "http://hmsoftware.org/hs";
                var data = new HSDto { Text = q };

                try
                {
                    var response = await httpClient.PostAsJsonAsync(url, data);
                    var foo = await response.Content.ReadFromJsonAsync<HSDto>();

                    HSText = foo.Text;
                    HSScore = foo.Score;
                    HSPrediction = foo.Prediction;

                }
                catch (Exception ex)
                {
                    HSText = "Sorry there was a problem - please try again later";
                    HSScore = "";
                    HSPrediction = "";
                    Log.Error($"Problem with HS webservice {ex}");
                }
            }

            else if (route == "auto")
            {
                Log.Information($"route is auto");
                //q ??= "https://twitter.com/dave_mateer/status/1505876265504546817";

                //var httpClient = new HttpClient();
                //var url = "http://hmsoftware.org/api/aa";

                //if (!ValidateUrl(q))
                //{
                //    AAText = $"Please check url: {q}";
                //    return Page();
                //}


                //try
                //{
                //    var data = new AADto { url = q };
                //    var response = await httpClient.PostAsJsonAsync(url, data);
                //    var foo = await response.Content.ReadFromJsonAsync<AADto>();

                //    AAText = "Processing";
                //    AAGuid = foo.guid.ToString();
                //    // redirect to aaresults
                //    return LocalRedirect($"/aaresults/{foo.guid}");


                //}
                //catch (Exception ex)
                //{
                //    AAText = "Sorry there was a problem - please try again later";
                //    Log.Error($"Problem with AA webservice {ex}");
                //}
            }

            return Page();
        }

        public async Task<IActionResult> OnPost(string q)
        {
            q ??= "https://twitter.com/dave_mateer/status/1505876265504546817";

            var httpClient = new HttpClient();
            var url = "http://hmsoftware.org/api/aa";

            if (!ValidateUrl(q))
            {
                AAText = $"Please check url: {q}";
                return Page();
            }

            try
            {
                var data = new AADto { url = q };
                var response = await httpClient.PostAsJsonAsync(url, data);
                var foo = await response.Content.ReadFromJsonAsync<AADto>();

                AAText = "Processing";
                AAGuid = foo.guid.ToString();
                // redirect to aaresults
                return LocalRedirect($"/aaresult/{foo.guid}");


            }
            catch (Exception ex)
            {
                AAText = "Sorry there was a problem - please try again later";
                Log.Error($"Problem with AA webservice {ex}");
            }
            // PRG
            return Page();
        }


        private bool ValidateUrl(string url)
        {
            //Uri validatedUri;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri validatedUri)) //.NET URI validation.
            {
                //If true: validatedUri contains a valid Uri. Check for the scheme in addition.
                return (validatedUri.Scheme == Uri.UriSchemeHttp || validatedUri.Scheme == Uri.UriSchemeHttps);
            }
            return false;
        }

    }

    class AADto
    {
        public Guid? guid { get; set; }
        public string? url { get; set; }
        public string? cdn_url { get; set; }
        public string? screenshot { get; set; }
        public string? thumbnail { get; set; }
        public string status { get; set; }
    }

    class HSDto
    {
        public string Text { get; set; }
        public string? Score { get; set; }
        public string? Prediction { get; set; }
    }
}
