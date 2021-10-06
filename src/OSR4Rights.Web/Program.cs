using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace OSR4Rights.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Verbose
            // Debug
            // Information
            // Warning 
            // Error
            // Fatal
            // https://stackoverflow.com/questions/62341787/how-can-i-override-serilog-levels-differently-for-different-sinks

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()

                .Enrich.FromLogContext()

                // Includes Debug from Microsoft.AspNetCore (noisy)
                // useful for deep debugging
                // a lot of data!
                //.WriteTo.File($@"logs/debug.txt", rollingInterval: RollingInterval.Day)

                // x-info-with-framework (useful for debugging)
                // stuff like: [14:27:19 INF] Executing handler method OSR4Rights.Web.Pages.IndexModel.OnGet - ModelState is Valid
                //.WriteTo.Logger(lc => lc
                //    .MinimumLevel.Information()
                //    .Filter.ByExcluding("RequestPath in ['/health-check', '/health-check-db']")
                //    .WriteTo.File("logs/x-info-with-framework.txt", rollingInterval: RollingInterval.Day)
                //.WriteTo.Console()
                //)

                // info
                // framework minimum level is Warning (normal everyday looking at logs)
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information()
                    // don't want out health check pages logged every 5 minutes
                    //.Filter.ByExcluding("RequestPath in ['/health-check', '/health-check-db']")
                    // getting rid of duplicates log entries when a 500
                    .Filter.ByExcluding("SourceContext = 'Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware'")

                    // don't want: [14:29:57 INF] Executing handler method OSR4Rights.Web.Pages.IndexModel.OnGet - ModelState is Valid
                    .Filter.ByExcluding(logEvent =>
                        logEvent.Level < LogEventLevel.Warning &&
                        Matching.FromSource("Microsoft.AspNetCore").Invoke(logEvent))
                    .WriteTo.File("logs/info.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                )

                // Warning (bad things - Warnings, Error and Fatal)
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Warning()
                    // stopping duplicate stacktraces, see blog 2021/03/10/a11-serilog-logging-in-razor-pages
                    .Filter.ByExcluding("SourceContext = 'Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware'")
                    .WriteTo.File("logs/warning.txt", rollingInterval: RollingInterval.Day)
                )

               .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
