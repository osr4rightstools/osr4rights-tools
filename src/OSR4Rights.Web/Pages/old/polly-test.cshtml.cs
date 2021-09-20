using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OSR4Rights.Web.Pages
{

    public class PollyTestModel : PageModel
    {
        [BindProperty]
        public int NumberOfTries { get; set; }


        public async Task OnGet() { }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            //Parallel.For(0, NumberOfTries, i =>
            //{
            //    Db.InsertLogNotAsync(connectionString, 1, $"try {i}");
            //});

            for (int i = 0; i < NumberOfTries; i++)
            {
                Db.InsertLogNotAsyncWithRetry(connectionString, 1, $"try {i}");
            }


            return Page();
        }
    }
}
