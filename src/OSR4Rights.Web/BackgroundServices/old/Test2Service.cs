using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BackgroundServiceTest.BackgroundServices
{
    public class Test2Service : BackgroundService
    {
        private readonly Test2Channel _test2Channel;
        public Test2Service(Test2Channel test2Channel) => _test2Channel = test2Channel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Global exception handler for entire Test2Service
            try
            {
                await Task.Delay(2000, stoppingToken);
                Log.Information("Started Test2Service");

                await foreach (var filePathAndName in _test2Channel.ReadAllAsync(stoppingToken))
                {
                    // an outer try catch to get any exceptions thrown in the catch or finally of the inner
                    // and to make sure the service keeps reading from the channel
                    try
                    {
                        await DoWork(filePathAndName, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex,
                            "Outer service exception handler - something threw in the inner catch or finally. We want the await foreach channel reader to keep going");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Test2Service has stopped fatally!");
                // in .NET5 without this, it would not stop the host
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
                // .NET6 logs it and stops the host now (which is better than silently failing!)
            }
        }

        private static async Task DoWork(string filePathAndName, CancellationToken stoppingToken)
        {
            Log.Information($"got a message from the channel {filePathAndName}");
            try
            {
                await Task.Delay(4000, stoppingToken);

                Log.Information("inside try");

                throw new ApplicationException("TEST exception.. ");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Test2Service - in catch");

                // I want to update my job status in the Db to be Exception
                // and clean up, as we're in some unknown state now
                // but if something throws here I need an outer try catch
                throw new ApplicationException("A bad sql method here throwing");
                Log.Warning(ex, "unreachable");
            }
            finally
            {
                Log.Information("inside finally - will run even if an exception is thrown in the catch");
                // this hides the exception thrown above in the global try catch
                throw new ApplicationException("throwing from finally");
                Log.Information("unreachable");
            }

            Log.Information("End of foreach - should we awaiting the next one in the channel now");
        }
    }

}
