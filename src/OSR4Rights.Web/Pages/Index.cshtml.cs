using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    public class IndexModel : PageModel
    {
        public string? HSText { get; set; }
        public string? AAText { get; set; }
        public string? HSScore { get; set; }
        public string? HSPrediction { get; set; }

        public async Task OnGet(string? route, string? q)
        {
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
                    var response = await httpClient.PostAsJsonAsync<HSDto>(url, data);
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
                    Log.Error($"Problem with webservice {ex}");
                }
            }
            else if (route == "auto")
            {
                Log.Information($"route is auto");
                q ??= "https://twitter.com/dave_mateer/status/1505876265504546817";

                Log.Information($"q is {q}");

                AAText = $"Coming soon - archiving: {q}";
            }
        }
    }

    class HSDto
    {
        public string Text { get; set; }
        public string? Score { get; set; }
        public string? Prediction { get; set; }
    }
}
