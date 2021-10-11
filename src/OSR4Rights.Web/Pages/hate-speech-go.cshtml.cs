using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class HateSpeechGoModel : PageModel
    {
        private readonly HateSpeechFileProcessingChannel _boundedMessageChannel;

        public HateSpeechGoModel(HateSpeechFileProcessingChannel boundedMessageChannel) =>
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
            if (createdFileName == "x1TE1TIER1.csv" || createdFileName == "x2TESTfileTIER1.csv" || createdFileName == "x3multilingual-testTIER1.csv")
            {
                var origFileNameSample = createdFileName.Replace("TIER1", "");
                var jobIdSample = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSample, Db.JobTypeId.HateSpeech);
                Log.Information($"HS - Sample File - JobId is {jobIdSample}");

                var newOsrFileNameAndPathSample = Path.Combine(osrFileStorePath, $"job-{jobIdSample}.tmp");

                var examplePath = Path.Combine("wwwroot/sample-data/hatespeech/", origFileNameSample);

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
            Log.Information($"HS uploaded file is {createdFileName}");

            // we know where tus should have uploaded the file to
            var tusFileStorePath = AppConfiguration.LoadFromEnvironment().TusFileStorePath;

            var uploadedTusFileAndPath = Path.Combine(tusFileStorePath, createdFileName);

            // csvHelper
            // https://joshclose.github.io/CsvHelper/getting-started/

            // The CSV file can have any number of rows and any number of columns, as long as at least one of the columns has a header named Text (or text)
            // open from tusFilePath before copying to osrFilePath
            bool shouldContinue = false;
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    // We're only checking that there is a header column called Text or text
                    // letting everything else past
                    // as "1", "hate speech, here", "a comment"
                    // wont pass as am not mapping unknown columns
                    //BadDataFound = context =>
                    //{
                    //    shouldContinue = true;
                    //    //malformedRow = true;
                    //    // Do what you need to do with the malformed row. For example:
                    //    //errorRecsCollection.Add(context.Parser.RawRecord);
                    //}
                };

                using (var reader = new StreamReader(uploadedTusFileAndPath))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = csv.GetRecords<Foo>();
                    var foo = records.Count();
                    if (foo > 1)
                    {
                        Log.Information($"HS found {foo} records in the csv");
                        shouldContinue = true;
                    }
                    else if (foo == 1)
                        ErrorMessage = "Please have more than 1 line of text to test";
                    else
                        ErrorMessage = "Found correct header but no records";
                }
            }
            catch (BadDataException ex)
            {
                ErrorMessage = $"Problem parsing the csv file with this row: {Environment.NewLine} {ex}";
            }
            catch (HeaderValidationException ex)
            {
                ErrorMessage = "Problem parsing the csv file - can't find Text or text column";
                Log.Information(ex, $"HS couldn't parse csv");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Unknown problem with the csv file";
                Log.Warning(ex, $"HS unknown problem with csv file");
            }


            if (!shouldContinue)
            {
                Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);

                return Page();
            }

            // sanity check the origFileName as this came from the user
            var origFileNameSanitised = FileHelper.ReplaceInvalidChars(origFileName);

            // get next jobId from Db
            var jobId = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSanitised, Db.JobTypeId.HateSpeech);
            Log.Information($"HS JobId is {jobId}");

            // Copy from tusFileStore to osrFileStore
            var newOsrFileNameAndPath = Path.Combine(osrFileStorePath, $"job-{jobId}.tmp");

            // copy uploaded file to tempPath location as job-{jobId}.tmp
            System.IO.File.Copy(uploadedTusFileAndPath, newOsrFileNameAndPath);

            Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);

            // add the job to the processing queue // eg job-17.tmp // so we know the jobId
            // **TODO put back in**
            await _boundedMessageChannel.AddFileAsync(newOsrFileNameAndPath);

            Log.Information($"FS added {newOsrFileNameAndPath} to queue");

            return LocalRedirect($"/result/{jobId}");
        }

        // The shape of the uploaded csv must contain Text or text header column
        public class Foo
        {
            public string Text { get; set; }
        }
    }
}
