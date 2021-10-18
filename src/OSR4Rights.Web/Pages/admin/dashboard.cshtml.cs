using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSR4Rights.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        public List<Dashboard500VM> Dashboard500Vms { get; set; } = null!;
        public List<Dashboard404VM> Dashboard404VMs { get; set; } = null!;
        public List<DashboardLoginAndJob> DashboardLoginsAndJobs { get; set; } = null!;
        public List<DashboardRealPage> DashboardRealPages { get; set; } = null!;
        public List<DashboardRequest> DashboardAllRequests { get; set; } = null!;

        public int TotalFaceSearchJobs { get; set; }
        public string TotalFaceSearchVMProcessingTimeInHHMMSS { get; set; } = null!;

        public int TotalHateSpeechJobs { get; set; }
        public string TotalHateSpeechVMProcessingTimeInHHMMSS { get; set; } = null!;

        public async Task OnGet()
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            Dashboard500Vms = await Db.GetDashboard500VMs(connectionString);

            Dashboard404VMs = await Db.GetDashboard404VMs(connectionString);

            DashboardLoginsAndJobs = await Db.GetDashboardLoginsAndJobs(connectionString);

            //TotalFaceSearchJobs = (await Db.GetDashboardLoginsAndJobs(connectionString))
            //    .Count(x => x.JobTypeId == 1);

            // todo - do in sql
            //var totalTimeTakenForFaceSearchJobs = (await Db.GetDashboardLoginsAndJobs(connectionString))
            //    .Where(x => x.JobTypeId == 1)
            //    .Select(x => x.TimeTakenInS)
            //    .Sum();
            //var time = TimeSpan.FromSeconds(totalTimeTakenForFaceSearchJobs);
            //TotalFaceSearchVMProcessingTimeInHHMMSS = time.ToString(@"hh\:mm\:ss");


            //var totalHateSpeechJobs = (await Db.GetDashboardLoginsAndJobs(connectionString))
            //     .Count(x => x.JobTypeId == 2);
            //TotalHateSpeechJobs = totalHateSpeechJobs;

            //var totalTimeTakenForHateSpeechJobs = (await Db.GetDashboardLoginsAndJobs(connectionString))
            //    .Where(x => x.JobTypeId == 2)
            //    .Select(x => x.TimeTakenInS)
            //    .Sum();
            //var time2 = TimeSpan.FromSeconds(totalTimeTakenForHateSpeechJobs);
            //TotalHateSpeechVMProcessingTimeInHHMMSS = time2.ToString(@"hh\:mm\:ss");


            DashboardRealPages = await Db.GetDashboardRealPages(connectionString);

            DashboardAllRequests = await Db.GetDashboardAllRequests(connectionString);
        }
    }
}
