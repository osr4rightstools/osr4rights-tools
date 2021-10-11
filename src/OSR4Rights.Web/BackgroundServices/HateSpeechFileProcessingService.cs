using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Hosting;
using PostmarkDotNet;
using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog;

namespace OSR4Rights.Web.BackgroundServices
{
    public class HateSpeechFileProcessingService : BackgroundService
    {
        private readonly HateSpeechFileProcessingChannel _hateSpeechFileProcessingChannel;
        public HateSpeechFileProcessingService(HateSpeechFileProcessingChannel boundedMessageChannel) => _hateSpeechFileProcessingChannel = boundedMessageChannel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Global exception handler 
            try
            {
                await Task.Delay(2200, stoppingToken);

                Log.Information($"Started 2.{nameof(HateSpeechFileProcessingService)}");

                await foreach (var filePathAndName in _hateSpeechFileProcessingChannel.ReadAllAsync(stoppingToken))
                {
                    // Outer try catch to get any exceptions thrown in the DoWork method
                    // and to make sure the service keeps reading from the channel
                    try
                    {
                        await DoWork(filePathAndName, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{nameof(HateSpeechFileProcessingService)} Outer exception handler - something threw in the {nameof(DoWork)} method's catch or finally. We want the await foreach channel reader to keep going");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning($"{nameof(HateSpeechFileProcessingService)} Global - Operation cancelled - can happen when app is shutting down gracefully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{nameof(HateSpeechFileProcessingService)} Global - stopped fatally!");
            }
        }

        private static async Task DoWork(string osrFileStorePathAndTempFileName, CancellationToken stoppingToken)
        {
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // The catch / finally blocks will want to delete the VM on exception so need these 3 variables in scope
            ResourceGroupsOperations resourceGroupClient = null!;
            string? resourceGroupName = null;
            int vmId = 0;
            int jobId = 0;

            try
            {
                // eg osrFileStorePathAndTempFileName is c:\osrFileStore\job-208.tmp
                // which we shall delete at the end of this file
                Log.Information($"{nameof(HateSpeechFileProcessingService)} found new osrFileStorePathAndTempFileName: {osrFileStorePathAndTempFileName} to process");

                // get the jobId by taking off .tmp so we have job-47
                var a = osrFileStorePathAndTempFileName.Replace(".tmp", "");
                // get the number is 208
                var b = a.Split("job-").Last();

                var result = int.TryParse(b, out int jobIdTemp);
                if (!result) throw new ApplicationException($"{nameof(HateSpeechFileProcessingService)} - Can't parse jobId, will show a warning message to user. See catch block");

                jobId = jobIdTemp;
                Log.Information($"JobId is {jobId}");

                var sw = Stopwatch.StartNew();

                var jobStatusId = await Db.GetJobStatusId(connectionString, jobId);

                if (jobStatusId != Db.JobStatusId.WaitingToStart) throw new ApplicationException("JobStatusId is not 1 - Waiting to start - logical problem");

                await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.Running);
                await LogHelper.LogToDbAndLog($"jobId {jobId} JobStatusId is now 2 - RunningJob", jobId);

                // Azure SDK credentials setup
                var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

                var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
                    new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

                var networkClient = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential(),
                    new NetworkManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

                var computeClient = new ComputeManagementClient(subscriptionId, new DefaultAzureCredential(),
                    new ComputeManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

                resourceGroupClient = resourcesClient.ResourceGroups;
                var virtualNetworksClient = networkClient.VirtualNetworks;
                var publicIpAddressClient = networkClient.PublicIPAddresses;
                var networkInterfaceClient = networkClient.NetworkInterfaces;
                var virtualMachinesClient = computeClient.VirtualMachines;

                //
                // 1. Is there an existing VM with VMStatusID 2 - ReadyToReceiveJobOnVM
                //
                var vmFromDb = await Db.GetFreeVMIfExists(connectionString, Db.VMTypeId.HateSpeechCPU);

                if (vmFromDb is { })
                {
                    vmId = vmFromDb.VMId;
                    resourceGroupName = vmFromDb.ResourceGroupName;

                    await LogHelper.LogToDbAndLog("HS Found an existing VM", jobId);

                    await Db.UpdateJobVMIdDetails(connectionString, jobId, vmFromDb.VMId);
                }
                else
                {
                    await LogHelper.LogToDbAndLog("HS Creating a new VM", jobId);

                    var location = "westeurope";
                    var usernameVM = "dave";

                    var passwordVM = PasswordCreator.Generate(32, 12);

                    // want the VMId to be used as the RGName eg webhatespeech123
                    vmFromDb = await Db.CreateNewVM(connectionString, passwordVM, Db.VMTypeId.HateSpeechCPU);

                    vmId = vmFromDb.VMId;
                    resourceGroupName = vmFromDb.ResourceGroupName!;

                    await Db.UpdateJobVMIdDetails(connectionString, jobId, vmFromDb.VMId);


                    // Resource Group
                    await LogHelper.LogToDbAndLog($"HS Creating resource group {resourceGroupName}...", jobId);

                    var resourceGroup = new ResourceGroup(location);
                    resourceGroup = await resourceGroupClient.CreateOrUpdateAsync(resourceGroupName, resourceGroup, stoppingToken);

                    // VNet
                    await LogHelper.LogToDbAndLog($"HS Creating vnet ...", jobId);

                    var vnet = new VirtualNetwork
                    {
                        Location = location,
                        AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "10.0.0.0/16" } },
                        Subnets = new List<Subnet> { new() { Name = "mySubnet", AddressPrefix = "10.0.0.0/24" } }
                    };
                    vnet = await virtualNetworksClient.StartCreateOrUpdate(resourceGroupName, "vnet", vnet)
                        .WaitForCompletionAsync(stoppingToken);

                    // Network Security Group
                    // interesting that port 80 is already open (are we using a basic public IP address then?)

                    // Public IP Address
                    var domainNameLabel = vmFromDb.ResourceGroupName;
                    await LogHelper.LogToDbAndLog($"HS Creating public IP address ...", jobId);

                    var ipAddress = new PublicIPAddress()
                    {
                        PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                        PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                        Location = location,
                        DnsSettings = new PublicIPAddressDnsSettings
                        { DomainNameLabel = domainNameLabel, Fqdn = domainNameLabel, ReverseFqdn = "" }
                    };
                    ipAddress = await publicIpAddressClient.StartCreateOrUpdate(resourceGroupName, "publicip", ipAddress)
                        .WaitForCompletionAsync();

                    // Nic
                    await LogHelper.LogToDbAndLog("HS Creating nic ...", jobId);

                    var nic = new NetworkInterface
                    {
                        Location = location,
                        IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                        {
                            new()
                            {
                                Name = "Primary",
                                Primary = true,
                                Subnet = new Subnet {Id = vnet.Subnets.First().Id},
                                PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                                PublicIPAddress = new PublicIPAddress {Id = ipAddress.Id}
                            }
                        }
                    };
                    nic = await networkInterfaceClient.StartCreateOrUpdate(resourceGroupName, "nic", nic).WaitForCompletionAsync();

                    // VM
                    await LogHelper.LogToDbAndLog("HS Creating VM ...", jobId);

                    var vm = new VirtualMachine(location)
                    {
                        NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                        {
                            // preview2 fix
                            //NetworkInterfaces = new[] { new NetworkInterfaceReference { Id = nic.Id } }
                            NetworkInterfaces = { new NetworkInterfaceReference() { Id = nic.Id } }
                        },
                        OsProfile = new OSProfile
                        {
                            ComputerName = resourceGroupName + "vm", // what the vm thinks it is called. 
                            AdminUsername = usernameVM,
                            AdminPassword = passwordVM,
                            LinuxConfiguration = new LinuxConfiguration
                            { DisablePasswordAuthentication = false, ProvisionVMAgent = true }
                        },
                        StorageProfile = new StorageProfile()
                        {
                            // get image version from url in portal eg
                            // https://portal.azure.com/#@27a85a82-0d78-4259-ae62-494068405286/resource/subscriptions/10cb0eb6-b1e9-40c6-b721-ee2a754f166c/resourceGroups/myGalleryRG/overview
                            // then click on the image version and copy to below (trimming bits)
                            ImageReference = new ImageReference()
                            {
                                Id = "/subscriptions/10cb0eb6-b1e9-40c6-b721-ee2a754f166c/resourceGroups/myGalleryRG/providers/Microsoft.Compute/galleries/myGallery/images/Wed8thSeptHateSpeechCPU/versions/21.09.08"
                            },
                            // preview2 fix
                            //DataDisks = new List<DataDisk>()
                        },
                        HardwareProfile = new HardwareProfile()
                        { VmSize = new VirtualMachineSizeTypes("Standard_D4s_v4") }
                        //{ VmSize = new VirtualMachineSizeTypes("Standard_D8s_v4") }
                    };

                    var operation = await virtualMachinesClient.StartCreateOrUpdateAsync(resourceGroupName, "vm", vm);
                    var vmFoo = (await operation.WaitForCompletionAsync()).Value;

                    //
                    // 1.5 Run script on VM to clone repo
                    //

                    await LogHelper.LogToDbAndLog($"HS Running git clone to get any updated hatespeech code ...", jobId);

                    // the name of the vm in Azure is VM
                    //var foo = await virtualMachinesClient.StartRunCommandAsync(resourceGroupName, "vm",
                    var foo = await virtualMachinesClient.StartRunCommandAsync(vmFromDb.ResourceGroupName, "vm",
                        new RunCommandInput("RunShellScript")
                        {
                            Script =
                            {
                                "sudo git clone https://github.com/khered20/Prepared_HateSpeech_Models /home/dave/hatespeech",
                                "sudo chown -R dave:dave /home/dave/hatespeech",
                                "sudo chmod +x /home/dave/hatespeech",
                                "cd /home/dave/hatespeech",
                                "wget https://functionsdm2storage.blob.core.windows.net/outputfiles/_all_train_results-20210712T152424Z-001.zip",
                                "unzip _all_train_results-20210712T152424Z-001.zip -d /home/dave/hatespeech"

                            }
                        });
                    var bar = await foo.WaitForCompletionAsync(stoppingToken);
                }

                // We now have an existing or new VM called vmFromDB
                await Db.UpdateVMStatusId(connectionString, vmFromDb.VMId, Db.VMStatusId.RunningJobOnVM);

                var password = vmFromDb.Password;
                var host = $"{vmFromDb.ResourceGroupName}.westeurope.cloudapp.azure.com";
                var username = "dave";

                await LogHelper.LogToDbAndLog("HS SFTP images zip file onto VM", jobId);

                //
                // 2. Sftp the uploaded file to the VM
                //

                // It is normal to retry here as we are waiting potentially for vm to be ready
                // if it is a newly created one
                var sftpRetryCount = 0;
                string fileNameCsv = "";
                while (sftpRetryCount < 11)
                {
                    if (sftpRetryCount == 10)
                    {
                        Log.Error("HS Can't connect to VM from SFTP - stopping");
                        throw new ApplicationException("Can't connect to VM after 10 retries");
                    }

                    var fullyCompleted = false;
                    await LogHelper.LogToDbAndLog("Trying 2.SFTP connect for upload", jobId);

                    // We know the file is saved in this format
                    string fileName = $"job-{jobId}.tmp";
                    fileNameCsv = $"job-{jobId}.csv";

                    using var client = new SftpClient(host, username, password);
                    try
                    {
                        client.Connect();
                        //var path = Path.GetTempPath();
                        var osrFileStorePath = AppConfiguration.LoadFromEnvironment().OsrFileStorePath;

                        var osrFileStorePathAndFileName = Path.Combine(osrFileStorePath, fileName);
                        Log.Information($"HS osrFileStorePathAndFileName is {osrFileStorePathAndFileName}");

                        var remoteFilePath = $@"/home/dave/hatespeech/{fileName}";
                        Log.Information($"remoteFilePath is {remoteFilePath}");

                        using var s = File.OpenRead(osrFileStorePathAndFileName);
                        client.UploadFile(s, remoteFilePath);

                        var newRemoteFilePathWithCsv = remoteFilePath.Replace(".tmp", ".csv");
                        client.RenameFile(remoteFilePath, newRemoteFilePathWithCsv);

                        fullyCompleted = true;
                        Log.Information("2.SFTP successful upload");
                    }

                    // Most normal exception first
                    // then exceptions I've seen which we want to trap

                    // When the machine isn't there we get this
                    catch (SocketException ex)
                    {
                        Log.Information(ex, "SocketException in sftp");
                        await Task.Delay(5000, stoppingToken);
                        sftpRetryCount++;
                    }
                    // Normal catch after socketexception waiting to connect
                    catch (SshAuthenticationException ex)
                    {
                        Log.Error(ex, "SshAuthenticationException in sftp");
                        await Task.Delay(5000, stoppingToken);
                        sftpRetryCount++;
                    }
                    // Maybe filesystem locally isn't ready yet with a large file.
                    catch (SftpPathNotFoundException ex)
                    {
                        Log.Error(ex, "SftpPathNotFoundException in sftp");
                        await Task.Delay(5000, stoppingToken);
                        sftpRetryCount++;
                    }
                    // Maybe Azure not ready yet
                    catch (FileNotFoundException ex)
                    {
                        Log.Error(ex, "FileNotFoundsException in sftp");
                        await Task.Delay(5000, stoppingToken);
                        sftpRetryCount++;
                    }
                    // When it is not ready can happen
                    catch (SftpPermissionDeniedException ex)
                    {
                        Log.Error(ex, "SftpPermissionDenied in sftp");
                        await Task.Delay(5000, stoppingToken);
                        sftpRetryCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception in SFTP");
                        throw;
                    }
                    finally
                    {
                        client.Disconnect();
                    }

                    if (fullyCompleted) break; // out of while
                }


                //
                // 3.SSH run the python job on the VM
                //
                var retryCount = 0;
                while (retryCount < 10)
                {
                    var fullyCompleted = false;
                    using var client = new SshClient(host, username, password);
                    // need this otherwise will timeout after 10 minutes or so
                    client.KeepAliveInterval = TimeSpan.FromMinutes(1);
                    try
                    {
                        client.Connect();
                        using var shellStream = client.CreateShellStream("Tail", 0, 0, 0, 0, 1024);
                        shellStream.DataReceived += (o, e) =>
                        {
                            var responseFromVm = Encoding.UTF8.GetString(e.Data).Trim();
                            if (responseFromVm != "")
                            {
                                // Potentially lots of info coming back 
                                // so don't pollute log files
                                // but inserting into the db
                                Db.InsertLogNotAsyncWithRetry(connectionString, jobId, responseFromVm);
                            }
                        };

                        var number = vmFromDb.VMId;

                        // eg webhatespeechcpu4
                        //var rg = vmFromDb.ResourceGroupName;
                        var rgName = "webhatespeechcpu";

                        //var prompt = $"dave@webfacesearchgpu{number}vm:~$";
                        var prompt = $"dave@{rgName}{number}vm:~$";
                        var promptFSC = $"dave@{rgName}{number}vm:~/hatespeech$";
                        // make sure the prompt is there - regex not working yet
                        //var output = shellStream.Expect(new Regex(@"[$>]"));
                        shellStream.Expect(prompt);

                        shellStream.WriteLine("cd /home/dave/hatespeech");
                        shellStream.Expect(promptFSC);

                        Log.Information($"HS 3. Running python3 PreBERT.py -m xlm-roberta-base -d all_train -s {fileNameCsv} -fn hate_speech_result");
                        shellStream.WriteLine($"python3 PreBERT.py -m xlm-roberta-base -d all_train -s {fileNameCsv} -fn hate_speech_result");

                        while (true)
                        {

                            var foo = shellStream.Expect(promptFSC, TimeSpan.FromSeconds(30));
                            // Probably python still processing
                            if (foo == null)
                            {
                                // Make sure the VM is still there
                                if (client.IsConnected)
                                    Log.Information("HS VM connected, python not finished yet");
                                else
                                    // The VM has disappeared (can happen if max runtime has been reached)
                                    throw new Exception("HS Connection lost - VM disappeared?");
                            }
                            else
                            {
                                Log.Information("Python finished");
                                break; // out of while
                            }
                        }

                        fullyCompleted = true;
                    }
                    catch (SshOperationTimeoutException ex)
                    {
                        Log.Error(ex, "SshOperationTimeoutException in SSH");
                        await Task.Delay(5000, stoppingToken);

                        retryCount++;
                    }
                    // this is the normal timeout we expect before the machine comes up properly
                    catch (SocketException ex)
                    {
                        // probably the machine isn't ready yet
                        // ie network unreachable
                        Log.Error(ex, "SocketException in SSH");
                        await Task.Delay(5000, stoppingToken);

                        retryCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception in SSH");
                        throw; // do not continue to 4. Sftp etc.. but go to finally, then to outer catch
                    }
                    finally
                    {
                        client.Disconnect();
                    }

                    if (fullyCompleted) break; // out of while
                }

                //
                // 4. Sftp back the results ie download
                //
                //string htmlFileName;
                using var sftp = new SftpClient(host, username, password);
                try
                {
                    sftp.Connect();

                    // download html file from remote
                    {
                        string pathRemoteFile = "/home/dave/hatespeech/hate_speech_result.html";

                        // Path where the file should be saved once downloaded (locally)
                        //var pathLocalDestinationDirectory = Path.Combine(Environment.CurrentDirectory, $"downloads/{jobId}");
                        var pathLocalDestinationDirectory = Path.Combine("/mnt/osrshare/", $"downloads/{jobId}");

                        // create a new directory for the job results
                        Directory.CreateDirectory(pathLocalDestinationDirectory);

                        var htmlFileName = "results.html";
                        var pathLocalFile = Path.Combine(pathLocalDestinationDirectory, htmlFileName);
                        Log.Information($"Local path is {pathLocalFile}");

                        using (Stream fileStream = File.OpenWrite(pathLocalFile))
                            sftp.DownloadFile(pathRemoteFile, fileStream);

                        sftp.DeleteFile(pathRemoteFile);

                        Log.Information($"Html downloaded to {pathLocalFile}");
                    }

                    // download csv file from remote 
                    {
                        string pathRemoteFile = "/home/dave/hatespeech/hate_speech_result.csv";

                        // Path where the file should be saved once downloaded (locally)
                        //var pathLocalDestinationDirectory = Path.Combine(Environment.CurrentDirectory, $"downloads/{jobId}");
                        var pathLocalDestinationDirectory = Path.Combine("/mnt/osrshare/", $"downloads/{jobId}");

                        var htmlFileName = $"results{jobId}.csv";
                        var pathLocalFile = Path.Combine(pathLocalDestinationDirectory, htmlFileName);
                        Log.Information($"Local path is {pathLocalFile}");

                        using (Stream fileStream = File.OpenWrite(pathLocalFile))
                            sftp.DownloadFile(pathRemoteFile, fileStream);

                        Log.Information($"Csv downloaded to {pathLocalFile}");

                        // instead of SSH'ing to box to remove files as we did in FaceSearchFileProcessingService
                        // we can use sftp as only have a single file to delete
                        sftp.DeleteFile(pathRemoteFile);

                        Log.Information($"HS Remote file deleted {pathRemoteFile}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "HS Exception in SFTP download / delete");
                }
                finally
                {
                    sftp.Disconnect();
                }

                //
                // 5. Clean up /job on VM ready for next run
                //

                // done above in sftp

                //
                // 6. Completion
                //

                Log.Information($"HS Complete in {sw.Elapsed:mm\\:ss\\.ff} - results are ready");
                await Db.InsertLog(connectionString, jobId, $"Complete in {sw.Elapsed:mm\\:ss\\.ff} - results are ready");

                await Db.UpdateJobToStatusCompleted(connectionString, jobId);

                // we are finished this job, so set the VM to be ready to receive another job
                await Db.UpdateVMStatusId(connectionString, vmFromDb.VMId, Db.VMStatusId.ReadyToRunJobOnVM);

                // Send confirmation email that the job is done
                var url = $"https://osr4rightstools.org/result/{jobId}";
                var toEmailAddress = await Db.GetEmailByJobId(connectionString, jobId);
                var textBody = $@"Hi,
                    Your HateSpeech job has completed on OSR4RightsTools. Please see you results here: {url}
                    ";

                var htmlText = $@"<p>Hi,</p>
                    <p>Your HateSpeech job has completed on OSR4RightsTools. Please see you results here:</p>
                    <p><a href=""{url}"">{url}</a></p>
                    ";

                var subject = "OSR4RightsTools HateSpeech Job Complete";

                var osrEmail = new OSREmail(
                    ToEmailAddress: toEmailAddress,
                    Subject: subject,
                    TextBody: textBody,
                    HtmlBody: htmlText
                );

                var postmarkServerToken = AppConfiguration.LoadFromEnvironment().PostmarkServerToken;

                var response = await Email.Send(postmarkServerToken, osrEmail);

                if (response is null)
                {
                    // Calls to the client can throw an exception which is handled in the helper
                    // lets give more information here so can recreate messages if need to from log files
                    Log.Warning($"{nameof(HateSpeechFileProcessingService)} Email send problem which probably didn't send for email: {toEmailAddress}, {textBody}, {subject}");
                }

                if (response?.Status != PostmarkStatus.Success)
                {
                    Log.Warning($"{nameof(HateSpeechFileProcessingService)} Email send response status error: {response?.Status}, {toEmailAddress}, {textBody}, {subject}");
                }

                Log.Information($"HS End {nameof(HateSpeechFileProcessingService)}");
            }

            // We want OperationCancelledException to be caught here too (Eg if we stop the app gracefully whilst running this method), so will attempt to cleanup
            catch (Exception ex)
            {
                // careful to not throw any unhandled exceptions (unless it is serious eg db offline even after retries) in any code below as want it all to try to complete
                Log.Warning(ex, $"In catch in method {nameof(DoWork)} of {nameof(HateSpeechFileProcessingService)} as an Exception was thrown. Attempting to clean up");

                if (resourceGroupName is { })
                {
                    Log.Information($"Unknown state of VM so try to delete resourcegroupname: {resourceGroupName} on Azure");
                    // being very careful here as Azure can error
                    try
                    {
                        // even though we have a stoppingtoken, we want to try to cleanup
                        await resourceGroupClient.StartDeleteAsync(resourceGroupName);
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, $"Can't delete resource group {resourceGroupName}.");
                    }
                }

                if (vmId != 0)
                {
                    Log.Warning($"Update vmId {vmId} in our database to Deleted and set DateTimeUtcDeleted to now");
                    await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);
                    await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);
                }

                if (jobId != 0)
                {
                    Log.Warning($"Unknown state of job so try to update jobId: {jobId} jobstatusid to exception");

                    await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.Exception);

                    await Db.InsertLog(connectionString, jobId, "VMStatusId is Deleted, JobStatusId is Exception, VM Deleted on Azure");
                }
                else
                {
                    Log.Error($"Serious error - {nameof(HateSpeechFileProcessingService)} couldn't get jobId, so couldn't update UI. It will look like the job is still in the queue, but it isn't");
                    // have put a warning on the UI too (in /result), to get the user to try again.
                }
            }

            Log.Information($"{nameof(HateSpeechFileProcessingChannel)} in clean up - deleting uploaded file on webserver  osrFileStorePathAndTempFileName: {osrFileStorePathAndTempFileName}");
            try
            {
                File.Delete(osrFileStorePathAndTempFileName);
            }
            catch (Exception e)
            {
                Log.Warning(e, $"{nameof(HateSpeechFileProcessingChannel)} couldn't delete osrFileStorePathAndTempFileName on webserver: {osrFileStorePathAndTempFileName}");
            }

            if (jobId != 0)
            {
                Log.Information($"{nameof(HateSpeechFileProcessingChannel)} in cleanup updating DateTimeUtcJobEndedOnVm Job End Date");
                await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString, jobId);
            }

            Log.Information($"{nameof(HateSpeechFileProcessingChannel)} End of foreach and awaiting the next one in the channel now");
        }
    }
}
