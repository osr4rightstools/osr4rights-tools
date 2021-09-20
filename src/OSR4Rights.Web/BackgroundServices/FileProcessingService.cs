using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace OSR4Rights.Web.BackgroundServices
{
    public class FileProcessingService : BackgroundService
    {
        private readonly FileProcessingChannel _fileProcessingChannel;
        //private readonly IServiceProvider _serviceProvider;

        public FileProcessingService(FileProcessingChannel boundedMessageChannel, IServiceProvider serviceProvider)
        {
            _fileProcessingChannel = boundedMessageChannel;
            //_serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000, stoppingToken);
            Log.Information("Started FileProcessing Background Service");

            //using var scope = _serviceProvider.CreateScope();
            //var processor = scope.ServiceProvider.GetRequiredService<IResultProcessor>();
            //var _osrHub2 = scope.ServiceProvider.GetRequiredService<IHubContext<OSRHub>>();

            // can I send a message to the client via SignalR?
            //await _osrHub2.Clients.All.SendAsync("NewFileMessage", "hello", stoppingToken);

            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            // await asynchronously the availability of new data from the channel
            // in this case it is uploaded files (which are jobs)
            // eg job-17.zip
            await foreach (var filePathAndName in _fileProcessingChannel.ReadAllAsync())
            {
                // todo - Steve Gordons scoping wrt DI
                //using var scope = _serviceProvider.CreateScope();
                //var processor = scope.ServiceProvider.GetRequiredService<IResultProcessor>();

                try
                {

                    //await processor.ProcessAsync(stream);

                    // we have a new file to process
                    Log.Information($"Fileprocessor found new filepathandname {filePathAndName}");

                    var fileName = Path.GetFileName(filePathAndName);
                    var jobIdString = fileName.Replace("job-", "");
                    jobIdString = jobIdString.Replace(".tmp", "");

                    var jobId = int.Parse(jobIdString);
                    // hack
                    //continue;

                    ////await Db.UpdateJobToStatusRunning(connectionString, jobId);
                    //await Db.UpdateJobIdToStatusId(connectionString, jobId, Db.JobStatusId.Running);

                    //// FaceSearch
                    //// 1.spin in a new T4 using AZ CLI via Bash
                    //Log.Information("1. Spinning up GPU VM via AZ CLI using Bash");
                    //Log.Information($"AppDomain.CurrentDomain.BaseDirectory is {AppDomain.CurrentDomain.BaseDirectory}");
                    //Log.Information($"Environment.CurrentDirectory is {Environment.CurrentDirectory}");

                    ////var cmd = Cli.Wrap("infra.azcli");
                    //var file = Environment.CurrentDirectory + "/" + "BackgroundServices/scripts/infra.azcli";
                    //Log.Information($"file is {file}");
                    ////var cmd = Cli.Wrap(file);
                    ////try
                    ////{
                    ////    await foreach (var cmdEvent in cmd.ListenAsync(stoppingToken))
                    ////    {
                    ////        switch (cmdEvent)
                    ////        {
                    ////            case StartedCommandEvent started:
                    ////                //_output.WriteLine($"Process started; ID: {started.ProcessId}");
                    ////                Log.Information($"Process started; ID: {started.ProcessId}");
                    ////                break;
                    ////            case StandardOutputCommandEvent stdOut:
                    ////                //_output.WriteLine($"Out> {stdOut.Text}");
                    ////                Log.Information($"Out> {stdOut.Text}");
                    ////                break;
                    ////            case StandardErrorCommandEvent stdErr:
                    ////                //_output.WriteLine($"Err> {stdErr.Text}");
                    ////                Log.Warning($"Err> {stdErr.Text}");
                    ////                // it could be that we want to exit here if any of the script writes to stderr
                    ////                // make sure script debugging turned off ie no set -x
                    ////                break;
                    ////            case ExitedCommandEvent exited:
                    ////                //_output.WriteLine($"Process exited; Code: {exited.ExitCode}");
                    ////                Log.Information($"Process exited; Code: {exited.ExitCode}");
                    ////                break;
                    ////        }
                    ////    }
                    ////}
                    ////catch (OperationCanceledException e)
                    ////{
                    ////    Log.Error("Operation cancelled - can I recover / clean up?");
                    ////    await Db.UpdateJobToStatusException(connectionString, jobId);
                    ////    throw;
                    ////}
                    ////catch (Exception ex)
                    ////{
                    ////    Log.Error(ex, "Exception - this can be caused when the bash command returns a non 0 status");
                    ////    await Db.UpdateJobToStatusException(connectionString, jobId);
                    ////    throw;
                    ////}

                    ////Console.WriteLine($"Result: {result}");

                    //await Task.Delay(15000, stoppingToken);
                    //Log.Information($"Done building Azure CLI infrastructure - cloud-init will be running now");




                    //// 2.sftp the uploaded zip file to the newly created VM
                    //// start the stream of data coming back to display to the user
                    //// put stream of data onto the a new VMOutputChannel

                    //// todo - patch this into the sftp bit here
                    //// todo - this is simply an ssh tests
                    ////await using var stream = File.OpenRead(filePathAndName);

                    ////var number = 50;
                    ////var password = "3HW5r2bZFuAOihjRaVnQIG9bcB9Q3WibgM";

                    ////var host = $"osrfacesearchgpu{number}.westeurope.cloudapp.azure.com";
                    ////var username = "dave";

                    ////using var client = new SshClient(host, username, password);
                    ////try
                    ////{
                    ////    client.Connect();
                    ////    using var shellStream = client.CreateShellStream("Tail", 0, 0, 0, 0, 1024);
                    ////    shellStream.DataReceived += (o, e) => Log.Information(Encoding.UTF8.GetString(e.Data));

                    ////    var prompt = $"dave@osrfacesearchgpu{number}vm:~$";
                    ////    var promptFS = $"dave@osrfacesearchgpu{number}vm:~/facesearch$";
                    ////    // make sure the prompt is there - regex not working yet
                    ////    //var output = shellStream.Expect(new Regex(@"[$>]"));
                    ////    var output = shellStream.Expect(prompt);

                    ////    shellStream.WriteLine("cd facesearch");
                    ////    shellStream.Expect(promptFS);

                    ////    shellStream.WriteLine("./facesearch.py");
                    ////    shellStream.Expect(promptFS);

                    ////}
                    ////catch (Exception ex)
                    ////{
                    ////    Log.Error(ex, "Exception in SSH");
                    ////}
                    ////finally
                    ////{
                    ////    Log.Information("Disconnecting");
                    ////    client.Disconnect();
                    ////}

                    //// 3.ssh start the job

                    //// 4.sftp back the results

                    //await Db.UpdateJobToStatusCompleted(connectionString, jobId);
                    Log.Information($"  Completed processing {filePathAndName}");
                }
                finally
                {
                    Log.Information("in finally");
                    //Log.Information($"Service worker deleting temp file {filePathAndName}");
                    //File.Delete(filePathAndName); // Delete the temp file
                }
            }
            Log.Information("End");
        }

        internal static class EventIds
        {
            public static readonly EventId StartedProcessing = new EventId(100, "StartedProcessing");
            public static readonly EventId ProcessorStopping = new EventId(101, "ProcessorStopping");
            public static readonly EventId StoppedProcessing = new EventId(102, "StoppedProcessing");
            public static readonly EventId ProcessedMessage = new EventId(110, "ProcessedMessage");
        }

        //private static class Log
        //{
        //    private static readonly Action<ILogger, string, Exception> _processedMessage = LoggerMessage.Define<string>(
        //        LogLevel.Debug,
        //        EventIds.ProcessedMessage,
        //        "Read and processed message with ID '{MessageId}' from the channel.");

        //    public static void StartedProcessing(ILogger logger) => logger.Log(LogLevel.Trace, EventIds.StartedProcessing, "Started message processing service.");

        //    public static void ProcessorStopping(ILogger logger) => logger.Log(LogLevel.Information, EventIds.ProcessorStopping, "Message processing stopping due to app termination!");

        //    public static void StoppedProcessing(ILogger logger) => logger.Log(LogLevel.Trace, EventIds.StoppedProcessing, "Stopped message processing service.");

        //    public static void ProcessedMessage(ILogger logger, string messageId) => _processedMessage(logger, messageId, null);
        //}
    }

    public interface IResultProcessor
    {
        Task ProcessAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
