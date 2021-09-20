//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Compression;
//using System.Linq;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//using Azure.Identity;
//using Azure.ResourceManager.Compute;
//using Azure.ResourceManager.Compute.Models;
//using Azure.ResourceManager.Network;
//using Azure.ResourceManager.Network.Models;
//using Azure.ResourceManager.Resources;
//using Azure.ResourceManager.Resources.Models;
//using Microsoft.AspNetCore.SignalR;
//using Renci.SshNet;
//using Renci.SshNet.Common;
//using Serilog;

//namespace OSR4Rights.Web.Hubs
//{
//    public class OSRHub : Hub
//    {
//        // Javascript in /job/17 calls this method
//        // which returns a ChannelReader<string> - ie messages to the UI
//        // using ChannelReader to avoid try catch issues
//        // todo - pass in userid too for security check?
//        // todo - look at brokenlinkchecker for ways to get unique session guid
//        public ChannelReader<string> Counter(string jobId, CancellationToken cancellationToken)
//        {
//            // No limit to the size of this (so no backpressure)
//            // todo - what if a user presses button twice?
//            var channel = Channel.CreateUnbounded<string>();

//            // We don't want to await WriteItemsAsync, otherwise we'd end up waiting 
//            // for all the items to be written before returning the channel back to the client.
//            int.TryParse(jobId, out var jobIdInt);

//            _ = WriteItemsAsync(channel.Writer, jobIdInt, cancellationToken);

//            return channel.Reader;
//        }

//        private async Task WriteItemsAsync(ChannelWriter<string> writer, int jobId, CancellationToken cancellationToken)
//        {
//            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

//            Exception? localException = null;

//            // Top level exception handler

//            // The catch below will want to delete the VM so need these 3 variables in the outer scope
//            ResourceGroupsOperations resourceGroupClient = null!;
//            string resourceGroupName = "dummy";
//            int vmId = 0;

//            try
//            {
//                var sw = Stopwatch.StartNew();
//                string mout;

//                var jobStatusId = await Db.GetJobStatusId(connectionString, jobId);

//                if (jobStatusId != Db.JobStatusId.WaitingToStart)
//                    // Probably have done run already and have refreshed the page 
//                    return;

//                await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.Running);
//                await LogHelper.Log($"jobId {jobId} JobStatusId is now 2 - RunningJob", jobId);

//                // Azure SDK credentials setup
//                var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

//                var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
//                    new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

//                var networkClient = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential(),
//                    new NetworkManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

//                var computeClient = new ComputeManagementClient(subscriptionId, new DefaultAzureCredential(),
//                    new ComputeManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

//                resourceGroupClient = resourcesClient.ResourceGroups;
//                var virtualNetworksClient = networkClient.VirtualNetworks;
//                var publicIpAddressClient = networkClient.PublicIPAddresses;
//                var networkInterfaceClient = networkClient.NetworkInterfaces;
//                var virtualMachinesClient = computeClient.VirtualMachines;

//                //
//                // 1. Is there an existing VM with VMStatusID 2 - ReadyToReceiveJobOnVM
//                //
//                var vmFromDb = await Db.GetFreeVMIfExists(connectionString);

//                if (vmFromDb is { })
//                {
//                    vmId = vmFromDb.VMId;
//                    resourceGroupName = vmFromDb.ResourceGroupName;

//                    mout = "Found an existing VM";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    await Db.UpdateJobVMIdDetails(connectionString, jobId, vmFromDb.VMId);
//                }
//                else
//                {
//                    await LogHelper.Log("Creating a new VM", jobId);

//                    var location = "westeurope";

//                    var usernameVM = "dave";

//                    // todo - make random password
//                    //var passwordVM = "letmein22!!";
//                    var passwordVM = PasswordCreator.Generate(32, 12);


//                    // want the VMId to be used as the RGName eg webfacesearchgpu123
//                    vmFromDb = await Db.CreateNewVM(connectionString, passwordVM);

//                    vmId = vmFromDb.VMId;
//                    resourceGroupName = vmFromDb.ResourceGroupName!;

//                    await Db.UpdateJobVMIdDetails(connectionString, jobId, vmFromDb.VMId);


//                    // Resource Group
//                    mout = $"Creating resource group {resourceGroupName}...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    var resourceGroup = new ResourceGroup(location);
//                    resourceGroup = await resourceGroupClient.CreateOrUpdateAsync(resourceGroupName, resourceGroup, cancellationToken);

//                    // VNet
//                    mout = $"Creating vnet ...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    var vnet = new VirtualNetwork
//                    {
//                        Location = location,
//                        AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "10.0.0.0/16" } },
//                        Subnets = new List<Subnet> { new() { Name = "mySubnet", AddressPrefix = "10.0.0.0/24" } }
//                    };
//                    vnet = await virtualNetworksClient.StartCreateOrUpdate(resourceGroupName, "vnet", vnet)
//                        .WaitForCompletionAsync(cancellationToken);

//                    // Network Security Group
//                    // interesting that port 80 is already open (are we using a basic public IP address then?)

//                    // Public IP Address
//                    var domainNameLabel = vmFromDb.ResourceGroupName;
//                    mout = $"Creating public IP address ...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    var ipAddress = new PublicIPAddress()
//                    {
//                        PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
//                        PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
//                        Location = location,
//                        DnsSettings = new PublicIPAddressDnsSettings
//                        { DomainNameLabel = domainNameLabel, Fqdn = domainNameLabel, ReverseFqdn = "" }
//                    };
//                    ipAddress = await publicIpAddressClient.StartCreateOrUpdate(resourceGroupName, "publicip", ipAddress)
//                        .WaitForCompletionAsync();

//                    // Nic
//                    mout = "Creating nic ...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    var nic = new NetworkInterface
//                    {
//                        Location = location,
//                        IpConfigurations = new List<NetworkInterfaceIPConfiguration>
//                    {
//                        new()
//                        {
//                            Name = "Primary",
//                            Primary = true,
//                            Subnet = new Subnet {Id = vnet.Subnets.First().Id},
//                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
//                            PublicIPAddress = new PublicIPAddress {Id = ipAddress.Id}
//                        }
//                    }
//                    };
//                    nic = await networkInterfaceClient.StartCreateOrUpdate(resourceGroupName, "nic", nic)
//                        .WaitForCompletionAsync();

//                    // VM
//                    mout = "Creating VM ...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    var vm = new VirtualMachine(location)
//                    {
//                        NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
//                        {
//                            // preview2 fix
//                            //NetworkInterfaces = new[] { new NetworkInterfaceReference { Id = nic.Id } }
//                            NetworkInterfaces = { new NetworkInterfaceReference() { Id = nic.Id } }
//                        },
//                        OsProfile = new OSProfile
//                        {
//                            ComputerName = resourceGroupName + "vm", // what the vm thinks it is called. 
//                            AdminUsername = usernameVM,
//                            AdminPassword = passwordVM,
//                            LinuxConfiguration = new LinuxConfiguration
//                            { DisablePasswordAuthentication = false, ProvisionVMAgent = true }
//                        },
//                        StorageProfile = new StorageProfile()
//                        {
//                            ImageReference = new ImageReference()
//                            {
//                                Id = "/subscriptions/10cb0eb6-b1e9-40c6-b721-ee2a754f166c/resourceGroups/myGalleryRG/providers/Microsoft.Compute/galleries/myGallery/images/TueJul13/versions/13.07.0"
//                            },
//                            // preview2 fix
//                            //DataDisks = new List<DataDisk>()
//                        },
//                        HardwareProfile = new HardwareProfile()
//                        { VmSize = new VirtualMachineSizeTypes("Standard_NC4as_T4_v3") },
//                    };

//                    var operation = await virtualMachinesClient.StartCreateOrUpdateAsync(resourceGroupName, "vm", vm);
//                    var vmFoo = (await operation.WaitForCompletionAsync()).Value;
//                    //
//                    // 1.5 Run script on VM to clone repo
//                    //

//                    mout = $"Running git clone to get any updated facesearch code ...";
//                    await LogHelper.Log(mout, jobId);
//                    await writer.WriteAsync(mout, cancellationToken);

//                    // the name of the vm in Azure is VM
//                    //var foo = await virtualMachinesClient.StartRunCommandAsync(resourceGroupName, "vm",
//                    var foo = await virtualMachinesClient.StartRunCommandAsync(vmFromDb.ResourceGroupName, "vm",
//                        new RunCommandInput("RunShellScript")
//                        {
//                            Script =
//                            {
//                            //"sudo rm -rf /home/dave/facesearch",
//                            "sudo git clone https://github.com/spatial-intelligence/OSR4Rights /home/dave/facesearch",
//                            "chmod -R 777 /home/dave/facesearch"
//                            }
//                        });
//                    var bar = await foo.WaitForCompletionAsync(cancellationToken);
//                }

//                // We now have an existing or new VM called vmFromDB
//                await Db.UpdateVMStatusId(connectionString, vmFromDb.VMId, Db.VMStatusId.RunningJobOnVM);

//                var password = vmFromDb.Password;
//                var host = $"{vmFromDb.ResourceGroupName}.westeurope.cloudapp.azure.com";
//                var username = "dave";

//                mout = "SFTP images zip file onto VM";
//                await LogHelper.Log(mout, jobId);
//                await writer.WriteAsync(mout, cancellationToken);

//                //
//                // 2. Sftp the uploaded zip file to the VM
//                //

//                // the uploaded zip file in /tmp/job-17.tmp
//                // which will be uploaded to the vm in ~/facesearch/facesearch_cloud/job-17.tmp
//                string fileName = "";

//                // It is normal to retry here as we are waiting potentially for vm to be ready
//                // if it is a newly created one
//                var sftpRetryCount = 0;
//                while (sftpRetryCount < 11)
//                {
//                    if (sftpRetryCount == 10)
//                    {
//                        Log.Error("Can't connect to VM from SFTP - stopping");
//                        throw new ApplicationException("Can't connect to VM after 10 retries");
//                    }

//                    var fullyCompleted = false;
//                    await LogHelper.Log("Trying 2.SFTP connect for upload", jobId);


//                    // We know the file is saved in this format
//                    fileName = $"job-{jobId}.tmp";

//                    using var client = new SftpClient(host, username, password);
//                    try
//                    {
//                        client.Connect();
//                        var path = Path.GetTempPath();

//                        var localFilePath = Path.Combine(path, fileName);
//                        Log.Information($"localFilePath is {localFilePath}");

//                        var remoteFilePath = $@"/home/dave/facesearch/facesearch_cloud/{fileName}";
//                        Log.Information($"remoteFilePath is {remoteFilePath}");

//                        using var s = File.OpenRead(localFilePath);
//                        client.UploadFile(s, remoteFilePath);

//                        fullyCompleted = true;
//                        Log.Information("2.SFTP successful upload");
//                    }

//                    // Most normal exception first
//                    // then exceptions I've seen which we want to trap

//                    // When the machine isn't there we get this
//                    catch (SocketException ex)
//                    {
//                        Log.Information(ex, "SocketException in sftp");
//                        await Task.Delay(5000, cancellationToken);
//                        sftpRetryCount++;
//                    }
//                    // Normal catch after socketexception waiting to connect
//                    catch (SshAuthenticationException ex)
//                    {
//                        Log.Error(ex, "SshAuthenticationException in sftp");
//                        await Task.Delay(5000, cancellationToken);
//                        sftpRetryCount++;
//                    }
//                    // Maybe filesystem locally isn't ready yet with a large file.
//                    catch (SftpPathNotFoundException ex)
//                    {
//                        Log.Error(ex, "SftpPathNotFoundException in sftp");
//                        await Task.Delay(5000, cancellationToken);
//                        sftpRetryCount++;
//                    }
//                    // Maybe Azure not ready yet
//                    catch (FileNotFoundException ex)
//                    {
//                        Log.Error(ex, "FileNotFoundsException in sftp");
//                        await Task.Delay(5000, cancellationToken);
//                        sftpRetryCount++;
//                    }
//                    // When it is not ready can happen
//                    catch (SftpPermissionDeniedException ex)
//                    {
//                        Log.Error(ex, "SftpPermissionDenied in sftp");
//                        await Task.Delay(5000, cancellationToken);

//                        sftpRetryCount++;
//                    }
//                    catch (Exception ex)
//                    {
//                        Log.Error(ex, "Exception in SFTP");
//                        throw;
//                    }
//                    finally
//                    {
//                        // Delete our temp file which has just been sent to the VM
//                        File.Delete(fileName);
//                        client.Disconnect();
//                    }

//                    if (fullyCompleted) break; // out of while
//                }


//                //
//                // 3.SSH run the python job on the VM
//                //
//                var retryCount = 0;
//                while (retryCount < 10)
//                {
//                    var fullyCompleted = false;
//                    using var client = new SshClient(host, username, password);
//                    // need this otherwise will timeout after 10 minutes or so
//                    client.KeepAliveInterval = TimeSpan.FromMinutes(1);
//                    try
//                    {
//                        client.Connect();
//                        using var shellStream = client.CreateShellStream("Tail", 0, 0, 0, 0, 1024);
//                        shellStream.DataReceived += (o, e) =>
//                        {
//                            var responseFromVm = Encoding.UTF8.GetString(e.Data).Trim();
//                            if (responseFromVm != "")
//                            {
//                                Log.Information(responseFromVm);

//                                var result = writer.TryWrite(DateTime.Now.ToLongTimeString() + " " + responseFromVm);
//                                if (!result) Log.Error("can't write to channel");
//                            }
//                        };

//                        var number = vmFromDb.VMId;
//                        var prompt = $"dave@webfacesearchgpu{number}vm:~$";
//                        var promptFS = $"dave@webfacesearchgpu{number}vm:~/facesearch$";
//                        var promptFSC = $"dave@webfacesearchgpu{number}vm:~/facesearch/facesearch_cloud$";
//                        // make sure the prompt is there - regex not working yet
//                        //var output = shellStream.Expect(new Regex(@"[$>]"));
//                        shellStream.Expect(prompt);

//                        shellStream.WriteLine("cd /home/dave/facesearch/facesearch_cloud");
//                        shellStream.Expect(promptFSC);

//                        // in case we have a run which failed, so clean up (should never happen)
//                        shellStream.WriteLine("rm -rf job");
//                        shellStream.Expect(promptFSC);

//                        await writer.WriteAsync("Running FaceSearch now", cancellationToken);

//                        shellStream.WriteLine($"unzip {fileName} -d job");
//                        shellStream.Expect(promptFSC);

//                        shellStream.WriteLine("./faceservice_main.py -i job/ -j 123");
//                        shellStream.Expect(promptFSC);

//                        fullyCompleted = true;
//                    }
//                    catch (SshOperationTimeoutException ex)
//                    {
//                        var message = $"SshOperationTimeoutException in SSH {ex}";
//                        Log.Error(ex, "SshOperationTimeoutException in SSH");
//                        await writer.WriteAsync(message, cancellationToken);
//                        await Task.Delay(5000, cancellationToken);

//                        retryCount++;
//                    }
//                    // this is the normal timeout we expect before the machine comes up properly
//                    catch (SocketException ex)
//                    {
//                        // probably the machine isn't ready yet
//                        // ie network unreachable
//                        //var message = $"SocketException in SSH {ex}";
//                        Log.Error(ex, "SocketException in SSH");
//                        //await writer.WriteAsync(message, cancellationToken);
//                        await Task.Delay(5000, cancellationToken);

//                        retryCount++;
//                    }
//                    catch (Exception ex)
//                    {
//                        var message = $"Exception in SSH {ex}";
//                        Log.Error(ex, "Exception in SSH");
//                        await writer.WriteAsync(message, cancellationToken);
//                        throw;
//                    }
//                    finally
//                    {
//                        Log.Information("Disconnecting");
//                        client.Disconnect();
//                    }

//                    if (fullyCompleted) break; // out of while
//                }

//                //
//                // 4. Sftp back the results ie download
//                //
//                string resultsFileName;
//                using var sftp = new SftpClient(host, username, password);
//                try
//                {
//                    sftp.Connect();

//                    // download file from remote
//                    string pathRemoteFile = "/home/dave/facesearch/facesearch_cloud/job/results_123.zip";

//                    // Path where the file should be saved once downloaded (locally)
//                    var path = Path.Combine(Environment.CurrentDirectory, "wwwroot/downloads");
//                    resultsFileName = $"results{jobId}.zip";
//                    var pathLocalFile = Path.Combine(path, resultsFileName);
//                    Log.Information($"Local path is {pathLocalFile}");

//                    using (Stream fileStream = File.OpenWrite(pathLocalFile))
//                        sftp.DownloadFile(pathRemoteFile, fileStream);

//                    Log.Information($"Results downloaded to {pathLocalFile}");
//                }
//                catch (Exception e)
//                {
//                    Log.Error(e, "Exception in SFTP download");
//                    throw;
//                }
//                finally
//                {
//                    sftp.Disconnect();
//                }

//                //
//                // 5. Clean up /job on VM ready for next run
//                //
//                {
//                    using var client = new SshClient(host, username, password);
//                    try
//                    {
//                        client.Connect();
//                        using var shellStream = client.CreateShellStream("Tail", 0, 0, 0, 0, 1024);

//                        shellStream.DataReceived += (_, e) =>
//                        {
//                            var responseFromVm = Encoding.UTF8.GetString(e.Data).Trim();

//                            if (responseFromVm.Trim() != "")
//                            {
//                                var result = writer.TryWrite(responseFromVm);
//                                if (!result) Log.Error("can't write to channel");
//                            }
//                        };

//                        var number = vmFromDb.VMId;
//                        var prompt = $"dave@webfacesearchgpu{number}vm:~$";
//                        shellStream.Expect(prompt);

//                        // delete all data from job
//                        shellStream.WriteLine("rm -rf /home/dave/facesearch/facesearch_cloud/job");
//                        shellStream.Expect(prompt);

//                    }
//                    catch (Exception ex)
//                    {
//                        var message = $"Exception in SSH {ex}";
//                        Log.Error(ex, "Exception in SSH");
//                        await writer.WriteAsync(message, cancellationToken);
//                        throw;
//                    }
//                    finally
//                    {
//                        Log.Information("Disconnecting");
//                        client.Disconnect();
//                    }
//                }


//                mout = $"Complete in {sw.Elapsed:mm\\:ss\\.ff} - results are ready";
//                await writer.WriteAsync(mout, cancellationToken);
//                Log.Information(mout);
//                await Db.InsertLog(connectionString, jobId, mout);

//                await Db.UpdateJobToStatusCompleted(connectionString, jobId);

//                // Unzip the results file so can display raw html from webserver
//                var zipFile = Path.Combine(Environment.CurrentDirectory, $"wwwroot/downloads/{resultsFileName}");
//                var destinationDirectory = Path.Combine(Environment.CurrentDirectory, $"wwwroot/downloads/{jobId}/");

//                // create the directory eg job123
//                // web user has to have permissions on /downloads
//                Log.Information($"Creating directory {destinationDirectory}");
//                Directory.CreateDirectory(destinationDirectory);

//                // Extract
//                ZipFile.ExtractToDirectory(zipFile, destinationDirectory);

//                // this works
//                //var resourceGroupDeleteResult = resourceGroupClient.StartDelete(resourceGroupName);

//                // we are finished this job, so set the VM to be ready to receive another job
//                // set to VMStatusId of 2 - ReadyToRunJobOnVM
//                await Db.UpdateVMStatusId(connectionString, vmFromDb.VMId, Db.VMStatusId.ReadyToRunJobOnVM);
//            }
//            // top level task cancelled
//            catch (TaskCanceledException ex)
//            {
//                var mout = "TaskCancelled Exception";
//                Log.Warning(ex, mout);
//                await Db.InsertLog(connectionString, jobId, mout);

//                // Stream wont work as cancelled and not good to await as wont run past this.
//                //await writer.WriteAsync(mout, cancellationToken);
//                localException = ex;

//                // Update JobStatus
//                Log.Warning("updating jobstatusid to cancelledbyuser");
//                await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.CancelledByUser);

//                // Unknown state of the VM and resource group, so delete
//                Log.Warning($"Delete this resourceGroup: {resourceGroupName}");
//                await resourceGroupClient.StartDeleteAsync(resourceGroupName);


//                Log.Warning($"Update vmId {vmId} in our database to Deleted");
//                await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);

//                await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);

//                await Db.InsertLog(connectionString, jobId, "VMStatusId is Deleted, JobStatusId is CancelledByUser, VM Deleted on Azure");

//            }
//            // top level exception handling on Hub
//            catch (Exception ex)
//            {
//                var mout = "Inside WriteItemsAsync - Top level Exception caught - Deleting VM";

//                //  Stream works so send message back to UI
//                await writer.WriteAsync(mout, cancellationToken);
//                Log.Fatal(ex, mout);
//                localException = ex;

//                // Update job status 
//                Log.Fatal("updating jobstatusid to exception");
//                await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.Exception);


//                // Unknown state of the VM and resource group, so delete
//                Log.Warning($"Delete {resourceGroupName}");
//                await resourceGroupClient.StartDeleteAsync(resourceGroupName);


//                Log.Warning($"Update vmId {vmId} in our database to Deleted");
//                await Db.UpdateVMStatusId(connectionString, vmId, Db.VMStatusId.Deleted);

//                await Db.UpdateVMDateTimeUtcDeletedToNow(connectionString, vmId);

//                await Db.InsertLog(connectionString, jobId, "VMStatusId is Deleted, JobStatusId is Exception, VM Deleted on Azure");
//            }
//            finally
//            {
//                // are there any resources left to clean up?
//                // eg after a task cancel?
//                writer.Complete(localException);

//                Log.Information("In finally - updating DateTimeUtcJobEndedOnVm Job End Date");
//                await Db.UpdateJobIdDateTimeUtcJobEndedOnVM(connectionString, jobId);
//            }

//            Log.Information("End OSRHub");
//        }
//    }
//}
