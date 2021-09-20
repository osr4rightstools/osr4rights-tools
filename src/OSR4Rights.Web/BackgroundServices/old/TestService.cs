using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace OSR4Rights.Web.BackgroundServices
{
    public class TestService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000, stoppingToken);
            Log.Information("Started Background Service");
            var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

            //try
            //{
            //    while (!stoppingToken.IsCancellationRequested)
            //    {
            //        // task1
            //        // want it to run once every x seconds starting immediately
            //        var (lastRunStart, lastRunEnd) = await Db.GetBudgetAlert(connectionString);

            //        bool shouldRun = false;
            //        // eg for every minute need 60 here
            //        var frequencyInSeconds = 10;

            //        // happy path
            //        if (DateTime.Now > lastRunEnd.AddSeconds(frequencyInSeconds)) shouldRun = true;

            //        if (shouldRun)
            //        {
            //            Log.Information("Start task 1");
            //            await Db.UpdateBudgetAlertLastRunStart(connectionString);
            //            //await Task.Delay(10000, stoppingToken); // doing work, maybe sending emails
            //            await Foo.Bar(stoppingToken);

            //            Log.Information("End task 1");
            //            await Db.UpdateBudgetAlertLastRunEnd(connectionString);
            //        }

            //        // task2
            //        var (_, lastRunEndB) = await Db.GetBudgetAlertB(connectionString);

            //        bool shouldRunB = false;
            //        // eg for every minute we need 60 here
            //        var frequencyInSecondsB = 10;

            //        if (DateTime.Now > lastRunEndB.AddSeconds(frequencyInSecondsB)) shouldRunB = true;

            //        if (shouldRunB)
            //        {
            //            Log.Information("Start task 2");
            //            await Db.UpdateBudgetAlertLastRunStartB(connectionString);
            //            await Foo.BarB(stoppingToken);

            //            Log.Information("End task 2");
            //            await Db.UpdateBudgetAlertLastRunEndB(connectionString);
            //        }

            //        Log.Information("ping");
            //        await Task.Delay(5000, stoppingToken);
            //    }
            //}
            //catch (OperationCanceledException)
            //{
            //    Log.Warning("Operation cancelled - can happen when app is shutting down gracefully");
            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex, "Exception - this should never happen. We want to handle exceptions inside the services method if we wish to retry");
            //}
            //Log.Warning("Background Service stopped");
        }
    }

    public static class Foo
    {
        public static async Task Bar(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                throw new ApplicationException("blow up - our system should be able to handle this and retry");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Bar task threw an exception, but we want it to retry the next time it is due to run");
            }
        }

        public static async Task BarB(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                throw new ApplicationException("blow upB - our system should be able to handle this and retry");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "BarB task threw an exception, but we want it to retry the next time it is due to run");
            }
        }
    }
}
