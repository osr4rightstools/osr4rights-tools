using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages
{
    // Tier1 as they can run samples too
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class JobModel : PageModel
    {
        public int JobId { get; set; }
        public int JobStatusId { get; set; }
        public string JobStatus { get; set; } = null!;

        public List<LogSmall> Logs { get; set; }

        public async Task<IActionResult> OnGetAsync(int jobId)
        {
            // The Hub is kicked off from javascript
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            JobId = jobId;

            var jobStatusId = await Db.GetJobStatusId(connectionString, jobId);
            JobStatusId = jobStatusId;

            if (jobStatusId == Db.JobStatusId.WaitingToStart) JobStatus = "Waiting to Start and now Running";
            if (jobStatusId == Db.JobStatusId.Running) JobStatus = "Running";
            if (jobStatusId == Db.JobStatusId.Completed) JobStatus = "Completed";
            if (jobStatusId == Db.JobStatusId.CancelledByUser) JobStatus = "Cancelled by User";
            if (jobStatusId == Db.JobStatusId.Exception) JobStatus = "Exception";

            // if job is not waiting to start, get the log files
            if (jobStatusId != Db.JobStatusId.WaitingToStart)
            {
                var logs = await Db.GetLogsForJobId(connectionString, jobId);
                Logs = logs;
            }
            else
            {
                Logs = new List<LogSmall>();
            }

            return Page();
        }
    }
}
