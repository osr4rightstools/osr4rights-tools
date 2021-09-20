using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace OSR4Rights.Web.BackgroundServices
{
    public class FaceSearchCleanUpAzureService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Global FaceSearchCleanUpAzureService exception handler 
            try
            {
                await Task.Delay(3000, stoppingToken);

                Log.Information($"Started 1x.{nameof(FaceSearchCleanUpAzureService)}");

                var timeBetweenChecks = TimeSpan.FromMinutes(5);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await DoWork(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // any exceptions in DoWork bubble up here
                        Log.Error(ex,
                            $"{nameof(FaceSearchCleanUpAzureService)} Outer exception handler - something threw in the {nameof(DoWork)} method's catch or finally. We want the loop to keep going, so it will try again after {timeBetweenChecks.TotalMinutes} minutes");

                        // swallow exception and continue the loop
                    }

                    await Task.Delay(timeBetweenChecks, stoppingToken);
                }
            }
            // When app shuts down gracefully it will trigger this
            catch (OperationCanceledException)
            {
                Log.Warning($"{nameof(FaceSearchCleanUpAzureService)} Global - Operation cancelled - can happen when app is shutting down gracefully. The service is not running now.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{nameof(FaceSearchCleanUpAzureService)} Global - Stopped fatally! The service is not running now.");
            }
        }

        private static async Task DoWork(CancellationToken stoppingToken)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
                new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

            var resourceGroupClient = resourcesClient.ResourceGroups;

            var rgPrefix = "webfacesearchgpu";

            // get a list from Azure of all faceSearch Rg's eg webfacesearchgpu123
            var rgs = resourceGroupClient.List().Where(x => x.Name.StartsWith(rgPrefix));

            //if (!rgs.Any()) Log.Information("No FaceSearch rg's found starting with: webfacesearchgpu");

            foreach (var rg in rgs)
            {
                var shouldDeleteVM = true;
                var shouldDeleteVMB = true;

                // eg webfacesearchgpu123
                // so we have the 123. As this is the same as teh VMId in our db
                var vmIdString = rg.Name.Replace(rgPrefix, "");
                var vmIdParseResult = int.TryParse(vmIdString, out int vmId);

                if (vmIdParseResult)
                {
                    var mostRecentJobOnVm = await Db.GetMostRecentJobOnVmId(connectionString, vmId);

                    if (mostRecentJobOnVm is { })
                    {
                        // Rule 1
                        // x minute window to leave Rg either waiting for more work, or continue processing, since last log entry
                        var timeToKeepVmSinceLastLogEntry = TimeSpan.FromMinutes(15);

                        var mostRecentLogDateTimeUtc =
                            await Db.GetMostRecentLogDateTimeUtcForMostRecentJobRunningOnVmId(connectionString,
                                vmId);

                        if (mostRecentLogDateTimeUtc is { })
                        {
                            var timeSinceLastLogEntry = DateTime.UtcNow - (DateTime)mostRecentLogDateTimeUtc;
                            if (timeSinceLastLogEntry < timeToKeepVmSinceLastLogEntry)
                            {
                                Log.Information($"FS Rule 1 found rg {rg.Name} leaving as time since last log entry is {timeSinceLastLogEntry.TotalMinutes:F1} minutes and we have a window of {timeToKeepVmSinceLastLogEntry.TotalMinutes} minutes");
                                shouldDeleteVM = false;
                            }
                            else
                            {
                                // Normal control flow - no jobs for x minutes
                                Log.Information( $"FS Rule 1 Deleting rg {rg.Name} as time since last log entry is {timeSinceLastLogEntry.TotalMinutes:F1} minutes which is greater than the {timeToKeepVmSinceLastLogEntry.TotalMinutes} minute window");

                                // it's possible the job has stalled on the VM and hasn't written a log file in the timeperiod
                                // so lets update our db as we need to delete the vm
                                if (mostRecentJobOnVm?.JobStatusId == Db.JobStatusId.Running)
                                {
                                    Log.Information($"FS Rule 1 Deleting rg {rg.Name} and updating Job Status from Running to Exception as no log messages for time window {timeToKeepVmSinceLastLogEntry.TotalMinutes:F1} minutes");

                                    await Db.UpdateJobIdToStatusId(connectionString, mostRecentJobOnVm.JobId, Db.JobStatusId.Exception);
                                    await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString, mostRecentJobOnVm.JobId);
                                }
                            }
                        }
                        else
                            Log.Information($"FS Rule 1 Deleting {rg.Name} as no log entries which is unusual");

                        // Rule 2
                        // max runtime if job is still running (and has written a log file before rule 1 deletes the VM) 
                        var maxRuntimeOfJob = TimeSpan.FromMinutes(120);

                        if (mostRecentJobOnVm?.DateTimeUtcJobEndedOnVm is { })
                        {
                            Log.Information("FS Rule 2 most recent job finished so don't need to check max runtime");
                            shouldDeleteVMB = false;
                        }
                        else
                        {
                            var timeSpanSinceJobStarted = DateTime.UtcNow - ((DateTime)mostRecentJobOnVm!.DateTimeUtcJobStartedOnVm)!;
                            if (timeSpanSinceJobStarted < maxRuntimeOfJob)
                            {
                                Log.Information(
                                    $"FS Rule 2 found rg {rg.Name} and leaving as job only been running for {timeSpanSinceJobStarted.TotalMinutes:F1} minutes and we have a maxRuntime of {maxRuntimeOfJob.TotalMinutes} minutes");
       
                                shouldDeleteVMB = false;
                            }
                            else
                            {
                                Log.Information(
                                    $"FS Rule 2 deleting rg {rg.Name} and updating job to Exception as job has been running for  {timeSpanSinceJobStarted.TotalMinutes:F1} minutes which is greater than the {maxRuntimeOfJob.TotalMinutes} minutes maximum runtime");

                                await Db.UpdateJobIdToStatusId(connectionString, mostRecentJobOnVm.JobId,
                                    Db.JobStatusId.Exception);
                                await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString,
                                    mostRecentJobOnVm.JobId);
                            }
                        }
                    }
                    else
                        Log.Error($"FS can't get most recent job on VM from Db with vmId {vmId}, so deleting the rg. Unusual");
                }
                else
                    Log.Error($"Can't parse vmId from Azure - {vmIdString} - PROBLEM! Deleting rg anyway");

                // if either rules say we should delete, then delete
                if (shouldDeleteVM || shouldDeleteVMB)
                {
                    Log.Information($"FS Deleting Rg: {rg.Name}");

                    try
                    {
                        await resourceGroupClient.StartDeleteAsync(rg.Name, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"{nameof(FaceSearchCleanUpAzureService)} and method {nameof(DoWork)} catch - threw an Exception while deleting rg {rg.Name}. This can happen when the Rg has been deleted already. Continuing as if it had worked");
                        // swallow exception and continue on
                    }

                    Log.Information($"{nameof(FaceSearchFileProcessingService)} Update vmId {vmId} in our database to Deleted, and DateTimeUtcDeleted");
                    await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);

                    await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);
                }
            }
        }
    }
}
//         // ssh -o StrictHostKeyChecking=no dave@webfacesearchgpu76.cloudapp.azure.com

