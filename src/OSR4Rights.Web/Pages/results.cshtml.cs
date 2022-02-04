using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSR4Rights.Web.BackgroundServices;

namespace OSR4Rights.Web.Pages
{
    [Authorize]
    public class ResultsModel : PageModel
    {
        private readonly FaceSearchFileProcessingChannel _faceSearchFileMessageChannel;
        private readonly HateSpeechFileProcessingChannel _hateSpeechFileProcessingChannel;
        private readonly SpeechPartsFileProcessingChannel _speechPartsFileProcessingChannel;

        public int JobId { get; set; }

        public List<JobViewModel> Jobs { get; set; } = null!;

        public int FaceSearchQueueLength { get; set; }
        public int HateSpeechQueueLength { get; set; }
        public int SpeechPartsQueueLength { get; set; }

        public ResultsModel(FaceSearchFileProcessingChannel faceSearchFileMessageChannel, HateSpeechFileProcessingChannel hateSpeechFileProcessingChannel, SpeechPartsFileProcessingChannel speechPartsFileProcessingChannel)
        {
            _faceSearchFileMessageChannel = faceSearchFileMessageChannel;
            _hateSpeechFileProcessingChannel = hateSpeechFileProcessingChannel;
            _speechPartsFileProcessingChannel = speechPartsFileProcessingChannel;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var loginId = Helper.GetLoginIdAsInt(HttpContext);

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var jobs = await Db.GetJobsForLoginId(connectionString, loginId);
            var listOfJobViewModels = new List<JobViewModel>();
            foreach (var job in jobs)
            {
                string? jobStatusString = null;

                if (job.JobStatusId == Db.JobStatusId.WaitingToStart) jobStatusString = "Waiting to Start";
                if (job.JobStatusId == Db.JobStatusId.Running) jobStatusString = "Running";
                if (job.JobStatusId == Db.JobStatusId.Completed) jobStatusString = "Completed";
                if (job.JobStatusId == Db.JobStatusId.CancelledByUser) jobStatusString = "Cancelled by User";
                if (job.JobStatusId == Db.JobStatusId.Exception) jobStatusString = "Exception";

                string? jobType = null;
                if (job.JobTypeId == Db.JobTypeId.FaceSearch) jobType = "FaceSearch";
                if (job.JobTypeId == Db.JobTypeId.HateSpeech) jobType = "HateSpeech";
                if (job.JobTypeId == Db.JobTypeId.SpeechParts) jobType = "SpeechParts";

                var jvm = new JobViewModel(job.JobId, job.LoginId, job.OrigFileName, job.DateTimeUtcUploaded,
                    job.JobStatusId, jobStatusString, job.VMId, job.DateTimeUtcJobStartedOnVm,
                    job.DateTimeUtcJobEndedOnVm, job.JobTypeId, jobType);

                listOfJobViewModels.Add(jvm);
            }
            Jobs = listOfJobViewModels;

            FaceSearchQueueLength = _faceSearchFileMessageChannel.CountOfFileProcessingChannel();
            HateSpeechQueueLength = _hateSpeechFileProcessingChannel.CountOfFileProcessingChannel();
            SpeechPartsQueueLength = _speechPartsFileProcessingChannel.CountOfSpeechPartsProcessingChannel();

            return Page();
        }
    }
}
