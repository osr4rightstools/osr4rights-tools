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
        public string? Text { get; set; }
        public string? Score { get; set; }
        public string? Prediction { get; set; }

        public async Task OnGet(string? q)
        {
            if (q == null) { }
            else
            {
                Log.Information($"q is {q}");

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

                    Text = foo.Text;
                    Score = foo.Score;
                    Prediction = foo.Prediction;

                }
                catch (Exception ex)
                {
                    Text = "Sorry there was a problem - please try again later";
                    Score = "";
                    Prediction = "";
                }
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
