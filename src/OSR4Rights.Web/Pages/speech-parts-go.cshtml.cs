using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CliWrap.EventStream;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSR4Rights.Web.BackgroundServices;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class SpeechPartsGoModel : PageModel
    {
        private readonly SpeechPartsFileProcessingChannel _boundedMessageChannel;

        public SpeechPartsGoModel(SpeechPartsFileProcessingChannel boundedMessageChannel) =>
            _boundedMessageChannel = boundedMessageChannel;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string createdFileName, string origFileName)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var osrFileStorePath = AppConfiguration.LoadFromEnvironment().OsrFileStorePath;

            var loginId = Helper.GetLoginIdAsInt(HttpContext);

            // Are they trying the sample files?
            // this block is an abbreviated version of the method below
            // suffix's of Sample to make scoping simpler
            if (createdFileName == "test_vocal_aTIER1.flac" || createdFileName == "test_vocal_aTIER1.mp3" || createdFileName == "test_vocal_aTIER1.mp4")
            {
                var origFileNameSample = createdFileName.Replace("TIER1", "");
                var jobIdSample = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSample, Db.JobTypeId.SpeechParts);
                Log.Information($"SP - Sample File - JobId is {jobIdSample}");

                var newOsrFileNameAndPathSample = Path.Combine(osrFileStorePath, $"job-{jobIdSample}.tmp");

                var examplePath = Path.Combine("wwwroot/sample-data/speechparts/", origFileNameSample);

                // copy from sample-data folder to osrFileStore location as job-{jobId}.tmp
                System.IO.File.Copy(examplePath, newOsrFileNameAndPathSample);

                await _boundedMessageChannel.AddFileAsync(newOsrFileNameAndPathSample);

                return LocalRedirect($"/result/{jobIdSample}");
            }

            // Only Tier2 or Admin are allowed to upload their own files
            var canUploadOwnFiles = false;
            foreach (var claim in User.FindAll(ClaimTypes.Role))
            {
                if (claim.Value == "Tier2") canUploadOwnFiles = true;
                else if (claim.Value == "Admin") canUploadOwnFiles = true;
            }

            if (!canUploadOwnFiles)
            {
                ErrorMessage = "Sorry, you need to be manually approved by use before you can upload your own files";
                return Page();
            }


            // eg uploaded file is 461d6c8d43f44d58b9f0b6772c436943
            Log.Information($"SP uploaded file is {createdFileName}");

            // we know where tus should have uploaded the file to
            var tusFileStorePath = AppConfiguration.LoadFromEnvironment().TusFileStorePath;

            var uploadedTusFileAndPath = Path.Combine(tusFileStorePath, createdFileName);

            // csvHelper
            // https://joshclose.github.io/CsvHelper/getting-started/

            // The CSV file can have any number of rows and any number of columns, as long as at least one of the columns has a header named Text (or text)
            // open from tusFilePath before copying to osrFilePath
            bool shouldContinue = false;
            // TODO take this out and put in file validation testing
            shouldContinue = true;
            //try
            //{
            //    var bad = new List<string>();
            //    var isRecordBad = false;
            //    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            //    {
            //        PrepareHeaderForMatch = args => args.Header.ToLower(),

            //        // parsing and getting bad data out
            //        // https://github.com/JoshClose/CsvHelper/issues/803
            //        // we are only checking for the existence of Text (or text) column
            //        // and that the rest of the csv is valid
            //        // ie the correct number of columns
            //        // escaping (which may look like more columns) eg no " when using a ,
            //        BadDataFound = context =>
            //        {
            //            isRecordBad = true;
            //            bad.Add(context.RawRecord);
            //        }
            //    };

            //    using (var reader = new StreamReader(uploadedTusFileAndPath))
            //    using (var csv = new CsvReader(reader, config))
            //    {
            //        //var records = csv.GetRecords<Foo>();

            //        var records = new List<Foo>();
            //        while (csv.Read())
            //        {
            //            var record = csv.GetRecord<Foo>();
            //            if (!isRecordBad)
            //            {
            //                records.Add(record);
            //            }
            //        }

            //        // are any of the records bad
            //        if (bad.Any())
            //        {
            //            // only care about the first error
            //            // getting confusing duplicates with 1 error too
            //            ErrorMessage = $"Problem around: {bad[0]}";
            //            Log.Warning($"User having csv parse errors {bad[0]} for file {uploadedTusFileAndPath}");
            //        }
            //        else
            //        {
            //            var foo = records.Count();
            //            if (foo > 0)
            //            {
            //                Log.Information($"HS found {foo} records in the csv");
            //                shouldContinue = true;
            //            }
            //            else
            //                ErrorMessage = "Found correct header but no records";
            //        }
            //    }
            //}
            //catch (HeaderValidationException ex)
            //{
            //    ErrorMessage = "Problem parsing the csv file - can't find Text or text column";
            //    Log.Information(ex, "HS couldn't parse csv");
            //}
            //catch (Exception ex)
            //{
            //    ErrorMessage = "Unknown problem with the csv file - check Logs";
            //    Log.Warning(ex, "HS unknown problem with csv file");
            //}


            //if (!shouldContinue)
            //{
            //    Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);

            //    return Page();
            //}



            // sanity check the origFileName as this came from the user
            var origFileNameSanitised = FileHelper.ReplaceInvalidChars(origFileName);

            // get next jobId from Db
            var jobId = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSanitised, Db.JobTypeId.SpeechParts);
            Log.Information($"SP JobId is {jobId}");

            // Copy from tusFileStore to osrFileStore
            var newOsrFileNameAndPath = Path.Combine(osrFileStorePath, $"job-{jobId}.tmp");

            // copy uploaded file to tempPath location as job-{jobId}.tmp
            System.IO.File.Copy(uploadedTusFileAndPath, newOsrFileNameAndPath);

            Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);

            // add the job to the processing queue // eg job-17.tmp // so we know the jobId
            // **TODO put back in**
            //await _boundedMessageChannel.AddFileAsync(newOsrFileNameAndPath);

            Log.Information($"SP added {newOsrFileNameAndPath} to queue");

            return LocalRedirect($"/result/{jobId}");
        }

        // The shape of the uploaded csv must contain Text or text header column
        public class Foo
        {
            public string Text { get; set; }
        }
    }
}
