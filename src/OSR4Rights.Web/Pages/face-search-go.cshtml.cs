using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSR4Rights.Web.BackgroundServices;
using Serilog;

namespace OSR4Rights.Web.Pages
{
    [Authorize(Roles = "Tier1, Tier2, Admin")]
    public class FaceSearchGoModel : PageModel
    {
        private readonly FaceSearchFileProcessingChannel _boundedMessageChannel;

        public FaceSearchGoModel(FaceSearchFileProcessingChannel boundedMessageChannel) =>
            _boundedMessageChannel = boundedMessageChannel;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string createdFileName, string? origFileName)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var osrFileStorePath = AppConfiguration.LoadFromEnvironment().OsrFileStorePath;

            var loginId = Helper.GetLoginIdAsInt(HttpContext);

            // Are they trying the sample files?
            // this block is an abbreviated version of the method below
            // suffix's of Sample to make scoping simpler
            if (createdFileName == "1barackTIER1.zip" || createdFileName == "6lfwsmallTIER1.zip")
            {
                var origFileNameSample = createdFileName.Replace("TIER1", "");
                var jobIdSample = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSample, Db.JobTypeId.FaceSearch);
                Log.Information($"FS - Sample File - jobIdSample is {jobIdSample}");

                var newOsrFileNameAndPathSample = Path.Combine(osrFileStorePath, $"job-{jobIdSample}.tmp");

                var examplePath = Path.Combine("wwwroot/sample-data/facesearch/", origFileNameSample);

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


            // eg uploaded fileName is 461d6c8d43f44d58b9f0b6772c436943
            Log.Information($"FS uploaded fileName on webserver is {createdFileName}");

            // we know where tus has uploaded the file to
            var tusFileStorePath = AppConfiguration.LoadFromEnvironment().TusFileStorePath;
            Log.Information($"FS uploaded path on webserver is tusFileStorePath {tusFileStorePath}");

            var uploadedTusFileAndPath = Path.Combine(tusFileStorePath, createdFileName);

            // create a new directory inside osrFileStorePath to unzip into to check validity
            var unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            var zipExtractPathInOsrFileStore = Path.Combine(osrFileStorePath, unixTime.ToString());

            Log.Information($"FS zipExtractPath to check validity of zip is {zipExtractPathInOsrFileStore}");

            try
            {
                // take the uploadedTusFileAndPath 
                // unzip to zipExtractPathInOsrFileStore
                ZipFile.ExtractToDirectory(uploadedTusFileAndPath, zipExtractPathInOsrFileStore);
            }
            catch (Exception ex)
            {
                Log.Information(ex, "FS - Couldn't extract zipfile to zipExtractPathInOsrFileStore");

                ErrorMessage = "Problem unzipping the file to zipExtractPathInOsrFileStore";

                // if this throws, it will swallow
                //System.IO.File.Delete(uploadedTusFileAndPath);

               Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);
                return Page();
            }

            // File Unzip worked

            // Make sure the file is the correct shape eg /search and /target
            var searchFolderExists = Directory.Exists(Path.Combine(zipExtractPathInOsrFileStore, "search"));
            if (!searchFolderExists)
            {
                ErrorMessage = "Can't find /search folder inside zip file";

                //System.IO.File.Delete(uploadedTusFileAndPath);
                Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);
                DeleteDirectory(zipExtractPathInOsrFileStore);
                return Page();
            }

            var targetFolderExists = Directory.Exists(Path.Combine(zipExtractPathInOsrFileStore, "target"));
            if (!targetFolderExists)
            {
                ErrorMessage = "Can't find /target folder inside zip file";

                //System.IO.File.Delete(uploadedTusFileAndPath);
               Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);
                DeleteDirectory(zipExtractPathInOsrFileStore);
                return Page();
            }

            // could check only correct filetypes eg .jpg etc..?

            // clean up - can delete the unzipped folder now
            DeleteDirectory(zipExtractPathInOsrFileStore);

            //
            // sanity check the origFileName as this came from the user
            //
            var origFileNameSanitised = FileHelper.ReplaceInvalidChars(origFileName);

            // get next jobId from Db
            var jobId = await Db.InsertJobWithOrigFileNameAndReturnJobId(connectionString, loginId, origFileNameSanitised, Db.JobTypeId.FaceSearch);
            Log.Information($"FS JobId is {jobId}");

            var newOsrFileNameAndPath = Path.Combine(osrFileStorePath, $"job-{jobId}.tmp");

            // copy uploadedTusFileAndPath to osrFileStore location as job-{jobId}.tmp
            System.IO.File.Copy(uploadedTusFileAndPath, newOsrFileNameAndPath, overwrite: true);

            //System.IO.File.Delete(uploadedTusFileAndPath);
            Helper.CleanUpTusFiles(tusFileStorePath, createdFileName);

            // add the job to the processing queue eg c:\osrFileStore\job-208.tmp
            await _boundedMessageChannel.AddFileAsync(newOsrFileNameAndPath);

            Log.Information($"FS added {newOsrFileNameAndPath} to queue");

            return LocalRedirect($"/result/{jobId}");
        }

      
        // https://stackoverflow.com/a/1703799/26086
        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
    }
}
