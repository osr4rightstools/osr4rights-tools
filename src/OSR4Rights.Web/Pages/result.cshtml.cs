using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSR4Rights.Web.BackgroundServices;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class ResultModel : PageModel
    {
        private readonly FaceSearchFileProcessingChannel _faceSearchFileMessageChannel;
        private readonly HateSpeechFileProcessingChannel _hateSpeechFileProcessingChannel;
        public JobViewModel Job { get; set; } = null!;
        public TimeSpan TotalTime { get; set; }
        public int QueueLength { get; set; }

        public List<LogSmall> Logs { get; set; } = null!;

        public string? WarningMessage { get; set; }

        public ResultModel(FaceSearchFileProcessingChannel faceSearchFileMessageChannel, HateSpeechFileProcessingChannel hateSpeechFileProcessingChannel)
        {
            _faceSearchFileMessageChannel = faceSearchFileMessageChannel;
            _hateSpeechFileProcessingChannel = hateSpeechFileProcessingChannel;
        }

        public async Task<IActionResult> OnGetAsync(int jobId)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;
            var loginId = Helper.GetLoginIdAsInt(HttpContext);

            // Is this Login allowed to look at this result?
            var isAllowed = await Db.CheckIfLoginIdIsAllowedToViewThisJobId(connectionString, loginId, jobId);

            if (!isAllowed) return LocalRedirect("/account/access-denied");

            var job = await Db.GetJobByJobId(connectionString, jobId);

            string? jobStatusString = job.JobStatusId switch
            {
                Db.JobStatusId.WaitingToStart => "Waiting to Start",
                Db.JobStatusId.Running => "Running",
                Db.JobStatusId.Completed => "Completed",
                Db.JobStatusId.CancelledByUser => "Cancelled by User",
                Db.JobStatusId.Exception => "Exception",
                _ => null
            };

            string? jobType = null;
            if (job.JobTypeId == Db.JobTypeId.FaceSearch) jobType = "FaceSearch";
            if (job.JobTypeId == Db.JobTypeId.HateSpeech) jobType = "HateSpeech";

            var jvm = new JobViewModel(job.JobId, job.LoginId, job.OrigFileName, job.DateTimeUtcUploaded,
                job.JobStatusId, jobStatusString, job.VMId, job.DateTimeUtcJobStartedOnVm,
                job.DateTimeUtcJobEndedOnVm, job.JobTypeId, jobType);

            Job = jvm;


            var logs = await Db.GetLogsForJobId(connectionString, jobId);
            Logs = logs;

            TimeSpan? totalTime = job.DateTimeUtcJobEndedOnVm - job.DateTimeUtcJobStartedOnVm;
            if (totalTime != null)
                TotalTime = ((TimeSpan)totalTime)!;
            else
            {
                if (job.DateTimeUtcJobStartedOnVm is { })
                {
                    // If the job has started but hasn't completed yet
                    TotalTime = ((TimeSpan)(DateTime.UtcNow - job.DateTimeUtcJobStartedOnVm))!;
                }
                else
                {
                    // Job hasn't started
                    TotalTime = TimeSpan.Zero;
                }
            }

            if (job.JobTypeId == Db.JobTypeId.FaceSearch) QueueLength = _faceSearchFileMessageChannel.CountOfFileProcessingChannel();
            if (job.JobTypeId == Db.JobTypeId.HateSpeech) QueueLength = _hateSpeechFileProcessingChannel.CountOfFileProcessingChannel();

            //if (job.JobTypeId == Db.JobTypeId.HateSpeech)
            //{
            // unusual situation where something has gone wrong.. 
            // do prompt the user
            if (job.JobStatusId == Db.JobStatusId.WaitingToStart && QueueLength == 0)
            {
                WarningMessage = "Please refresh this page.";

                Log.Information("Please refresh this page as it looks like the job is waiting to start, and is not in the queue. If this continues, please try submitting the job again, and otherwise contact us.");
            }
            //}

            return Page();
        }
    }
}
