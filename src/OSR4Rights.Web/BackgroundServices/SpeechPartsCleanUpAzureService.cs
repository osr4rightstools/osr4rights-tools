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
    public class SpeechPartsCleanUpAzureService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Global HateSpeechCleanUpAzureService exception handler 
            try
            {
                await Task.Delay(4500, stoppingToken);

                Log.Information($"Started 3x.{nameof(SpeechPartsCleanUpAzureService)}");

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
                            $"{nameof(SpeechPartsCleanUpAzureService)} Outer exception handler - something threw in the {nameof(DoWork)} method's catch or finally. We want the loop to keep going, so it will try again after {timeBetweenChecks.TotalMinutes} minutes");

                        // swallow exception and continue the loop
                    }

                    await Task.Delay(timeBetweenChecks, stoppingToken);
                }
            }
            // When app shuts down gracefully it will trigger this
            catch (OperationCanceledException)
            {
                Log.Warning($"{nameof(SpeechPartsCleanUpAzureService)} Global - Operation cancelled - can happen when app is shutting down gracefully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{nameof(SpeechPartsCleanUpAzureService)} Global - Stopped fatally! The service is not running now.");
            }
        }

        private static async Task DoWork(CancellationToken stoppingToken)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
                new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

            var resourceGroupClient = resourcesClient.ResourceGroups;

            //var rgPrefix = "webhatespeechcpu";
            var rgPrefix = "speechpartscpu";

            var rgs = resourceGroupClient.List().Where(x => x.Name.StartsWith(rgPrefix));

            foreach (var rg in rgs)
            {
                var shouldDeleteVM = true;

                var vmIdString = rg.Name.Replace(rgPrefix, "");
                var vmIdParseResult = int.TryParse(vmIdString, out int vmId);

                if (vmIdParseResult)
                {
                    var mostRecentJobOnVm = await Db.GetMostRecentJobOnVmId(connectionString, vmId);

                    if (mostRecentJobOnVm is { })
                    {
                        // Rule 1
                        // x minute window to leave Rg either waiting for more work, or continue processing, since last log entry
                        var timeToKeepVmSinceLastLogEntry = TimeSpan.FromMinutes(30);

                        var mostRecentLogDateTimeUtc = await Db.GetMostRecentLogDateTimeUtcForMostRecentJobRunningOnVmId(connectionString, vmId);

                        if (mostRecentLogDateTimeUtc is { })
                        {
                            var timeSinceLastLogEntry = DateTime.UtcNow - (DateTime)mostRecentLogDateTimeUtc;
                            if (timeSinceLastLogEntry < timeToKeepVmSinceLastLogEntry)
                            {
                                Log.Information($"SP Rule 1 Found rg {rg.Name} leaving as time since last log entry is {timeSinceLastLogEntry.TotalMinutes:F1} minutes and we have a window of {timeToKeepVmSinceLastLogEntry.TotalMinutes} minutes");
                  
                                shouldDeleteVM = false;
                            }
                            else
                            {
                                // Normal control flow - no jobs for x minutes
                                Log.Information($"SP Rule 1 Deleting rg {rg.Name} as time since last log entry is {timeSinceLastLogEntry.TotalMinutes:F1} minutes which is greater than {timeToKeepVmSinceLastLogEntry.TotalMinutes} minute window");

                                // it's possible the job is taking longer than window period 
                                if (mostRecentJobOnVm?.JobStatusId == Db.JobStatusId.Running)
                                {
                                    Log.Information($"SP Rule 1 - Deleting rg {rg.Name} and updating Job Status from Running to Exception as no log messages for time window {timeToKeepVmSinceLastLogEntry.TotalMinutes:F1} minutes");

                                    await Db.UpdateJobIdToStatusId(connectionString, mostRecentJobOnVm.JobId, Db.JobStatusId.Exception);
                                    await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString, mostRecentJobOnVm.JobId);
                                }
                            }
                        }
                        else
                            Log.Information($"SP Rule 1 Deleting {rg.Name} as no log entries which is unusual");

                        // Rule 2
                        // max runtime if job is still running (and has written a log file before rule 1 deletes the VM) 
                        // todo - get job writing to console then can use this rule
                        // need to get log writing to have this running!
                        //var maxRuntimeOfJob = TimeSpan.FromMinutes(120);

                        //if (mostRecentJobOnVm?.DateTimeUtcJobEndedOnVm is { })
                        //{
                        //    Log.Information("HS Rule 2 most recent job finished, so leave for rule 1 - 15 minute window rule");
                        //    shouldDeleteVMB = false;
                        //}
                        //else
                        //{
                        //    var timeSpan = DateTime.UtcNow - mostRecentJobOnVm!.DateTimeUtcJobStartedOnVm;
                        //    if (timeSpan < maxRuntimeOfJob)
                        //    {
                        //        Log.Information($"HS Rule 2 found rg {rg.Name} and leaving as job only been running for {timeSpan:hh\\:mm\\:ss} hh:mm:ss and we have a maxRuntime of {maxRuntimeOfJob:hh\\:mm\\:ss} hh:mm:ss");
                        //        shouldDeleteVMB = false;
                        //    }
                        //    else
                        //    {
                        //        Log.Information($"HS Rule 2 deleting rg {rg.Name} as job has been running for {timeSpan:hh\\:mm\\:ss} hh:mm:ss which is greater than the {maxRuntimeOfJob:hh\\:mm\\:ss} hh:mm:ss maximum runtime");

                        //        Log.Information($"HS Rule 2 - updating Job Status from Running to Exception as max runtime has been hit");
                        //        await Db.UpdateJobIdToStatusId(connectionString, mostRecentJobOnVm.JobId, Db.JobStatusId.Exception);
                        //        await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString, mostRecentJobOnVm.JobId);
                        //    }
                        //}
                        //shouldDeleteVMB = false;
                    }
                    else
                        Log.Error($"SP can't get most recent job on VM from Db with vmId {vmId}, so deleting the rg");
                }
                else
                    Log.Error($"SP can't parse vmId from Azure - {vmIdString} - PROBLEM! Deleting rg anyway");

                // if either rules say we should delete, then delete
                //if (shouldDeleteVM || shouldDeleteVMB)
                if (shouldDeleteVM)
                {
                    Log.Information($"SP Deleting Rg: {rg.Name}");
                    try
                    {
                        await resourceGroupClient.StartDeleteAsync(rg.Name, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"{nameof(SpeechPartsCleanUpAzureService)} and method {nameof(DoWork)} catch - threw an Exception while deleting rg {rg.Name}. This can happen when the Rg has been deleted already. Continuing as if it had worked");
                        // swallow exception and continue on
                    }

                    Log.Information($"{nameof(SpeechPartsCleanUpAzureService)} Update vmId {vmId} in our database to Deleted, and DateTimeUtcDeleted");
                    await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);

                    await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);
                }
            }
        }
    }
}

