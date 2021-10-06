using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OSR4Rights.Web.Pages
{

    public class PollyTest2Model : PageModel
    {
        [BindProperty]
        public int NumberOfTries { get; set; }

        public void OnPost()
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            Parallel.For(0, NumberOfTries, i => Db.InsertLogNotAsyncWithRetry(connectionString, 1, $"try {i}"));
        }
    }
}
