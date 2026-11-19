using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace OSR4Rights.Web;

public class Startup
{
    public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment, IWebHostEnvironment env)
    {
        _hostEnvironment = hostEnvironment;
        Configuration = configuration;
        CurrentEnvironment = env;
    }
    private readonly IHostEnvironment _hostEnvironment;
    public IConfiguration Configuration { get; }
    //https://stackoverflow.com/a/32556303/26086
    private IWebHostEnvironment CurrentEnvironment { get; set; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // persist cookies to disk so that if server restarts it can read them
        // and people wont have to login again if they have pressed remember me
        // https://stackoverflow.com/questions/56490525/asp-net-core-cookie-authentication-is-not-persistant
        // create a directory for keys if it doesn't exist
        // it'll be created in the root, beside the wwwroot directory
        //var parent = new DirectoryInfo(_hostEnvironment.ContentRootPath).Parent;

        //var foo = Path.Combine(parent!.ToString(), "osr-cookie-keys");

        // Create a directory for keys if it doesn't exist
        //if (!Directory.Exists(foo)) Directory.CreateDirectory(foo);

        var cookieKeyPath = AppConfiguration.LoadFromEnvironment().CookieKeyPath;

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(cookieKeyPath))
            .SetApplicationName("CustomCookieAuthentication");

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/account/access-denied");
                options.ReturnUrlParameter = "returnurl";
            });

        services.AddAuthorization();

        services.AddRazorPages();

        services.AddHttpContextAccessor();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Middleware
        // log all requests for custom logging
        app.Use(async (context, next) =>
        {
            // how long the request takes
            var watch = Stopwatch.StartNew();

            await next.Invoke();

            // This is the last in the pipeline

            // Cookie TEST
            string cookieValue = context.Request.Cookies[".AspNetCore.Cookies"];

            // ONE - grab the CookieAuthenticationOptions instance
            var opt = context.RequestServices
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme); //or use .Get("Cookies")

            // TWO - Get the encrypted cookie value
            var cookie = opt.CookieManager.GetRequestCookie(context, opt.Cookie.Name);

            if (cookie == null)
            {
            }
            else
            {
                // THREE - decrypt it
                var asdf = opt.TicketDataFormat.Unprotect(cookie);

                var issuedUtc = asdf.Properties.IssuedUtc;
                var expiresUtc = asdf.Properties.ExpiresUtc;

                var foo = asdf.Principal.Claims;

                // eg davemateer@gmail.com
                var name = foo.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
                // eg Tier1, Tier2, Admin
                var role = foo.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
                // eg 46
                var loginId = foo.FirstOrDefault(x => x.Type == "LoginId")?.Value;

                var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

                // Cookie table (for fire-map)
                await Db.InsertUpdateCookie(connectionString,
                    cookieValue,
                    loginId,
                    issuedUtc,
                    expiresUtc
                );

            }

            // Custom Logging
            var message = "";

            try
            {
                // dump all the headers
                //var headers = context.Request.Headers;
                //Log.Information(" --start request headers--");
                //foreach (var header in headers)
                //{
                //    Log.Information(header.Key + " : " + header.Value);
                //}
                //Log.Information(" --end request headers--");

                // maybe get the total bytes from nginx so can write that to db, and dashboard the total traffic

                // HTTP version passed from nginx
                var xDMRequest = context.Request.Headers.FirstOrDefault(x => x.Key == "X-DM-Request").Value;
                string httpVersion = "none";
                if (xDMRequest != StringValues.Empty)
                {
                    // get final HTTP/1.0 or 1.1 or 2
                    var bar = xDMRequest.ToString().Split(new[] { "HTTP" }, StringSplitOptions.None).Last();
                    httpVersion = $"HTTP{bar}";
                }

                // connection

                // this wont work as behind nginx
                //var remoteIpAddress = context.Connection.RemoteIpAddress;
                //var message = $"Remote IP address: {remoteIpAddress} ";

                // both of these seem to give the same correct IP
                // leave in for now.
                //var xForwardedFor = context.Request.Headers.FirstOrDefault(x => x.Key == "X-Forwarded-For");
                //message += $"xForwardedFor: {xForwardedFor} ";

                var ipFoo = context.Request.Headers.FirstOrDefault(x => x.Key == "X-Real-IP").Value;
                string? ipAddress = null;
                if (ipFoo != StringValues.Empty)
                    ipAddress = ipFoo;
                message += $"xRealIP: {ipAddress} ";

                // eg GET
                string verb = context.Request.Method;
                message += $"Method:  {verb} ";
                // page requested
                // eg http:
                //message += $"Scheme:  {context.Request.Scheme} ";
                // eg osr4rightstools.org
                //message += $"Host:  {context.Request.Host} ";

                // eg /hate-speech
                PathString path = context.Request.Path;
                message += $"Path:  {path} ";

                // eg ?123456
                QueryString qs = context.Request.QueryString;

                // converting to a nullable string for database
                string? queryString;
                if (qs == QueryString.Empty)
                    queryString = null;
                else
                    queryString = qs.ToString();
                message += $"QueryString:  {queryString} ";

                // response
                var statusCode = context.Response.StatusCode;
                message += $"StatusCode:  {context.Response.StatusCode} ";

                watch.Stop();

                var elapsedTimeInMs = (int)watch.ElapsedMilliseconds;
                message += $"Time:  {watch.ElapsedMilliseconds}ms ";

                // Request header: referer null if no referer
                Uri? referer = context.Request.GetTypedHeaders().Referer;

                string? refererS = null;
                if (referer is { })
                {
                    // 9th Dec 2021 took this out to make marketing dashboard processing easier
                    //if (referer.ToString().Contains(@"https://osr4rightstools.org/"))
                    //    refererS = referer.ToString().Replace(@"https://osr4rightstools.org/", "");
                    //else
                    refererS = referer.ToString();

                    message += $"Referer: {refererS} ";
                }


                // request header: User Agent
                var ua = context.Request.Headers.FirstOrDefault(x => x.Key == "User-Agent").Value;
                string? userAgent;
                if (string.IsNullOrEmpty(ua))
                    userAgent = null;
                else
                    userAgent = ua.ToString();
                message += $"UserAgent: {userAgent}";

                message += $"HttpVersion: {httpVersion} ";

                //message += $"TraceIdentifier: {context.TraceIdentifier} ";


                // loginId, email, role of logged in user if logged in
                // TraceIdentifier to correlate with initial log entry at the top
                // Had issues with iPhone Chrome not working on Strict

                // custom claim Type for LoginId eg 37
                string? loginIdString = context.User.Claims.FirstOrDefault(x => x.Type == "LoginId")?.Value;

                int? loginId = null;

                if (loginIdString != null)
                {
                    var result = int.TryParse(loginIdString, out int loginIdFoo);
                    if (result)
                        loginId = loginIdFoo;
                }

                // name eg davemateer@gmail.com 
                string? email = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

                // role eg Admin
                var roleName = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                message += $"loginId: {loginIdString} email: {email} role: {roleName} traceIdentifier: {context.TraceIdentifier}";

                Log.Debug(message);

                // pump data into a queue?
                // which will get written to a database
                // as we're async all the way up, try using straight db query

                var connectionString = AppConfiguration.LoadFromEnvironment().ConnectionString;

                int webLogTypeId = Db.WebLogTypeId.Page;

                if (path.ToString().StartsWith("/js"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/lib"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/css"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/images"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/sample-data"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/screenshots"))
                    webLogTypeId = Db.WebLogTypeId.Asset;
                else if (path.ToString().StartsWith("/health-check"))
                    webLogTypeId = Db.WebLogTypeId.HealthCheckPage;
                else if (path.ToString().StartsWith("/robots.txt"))
                    webLogTypeId = Db.WebLogTypeId.RobotsTxt;
                else if (path.ToString().StartsWith("/sitemap.xml"))
                    webLogTypeId = Db.WebLogTypeId.SitemapXml;
                else if (path.ToString().StartsWith("/favicon.ico"))
                    webLogTypeId = Db.WebLogTypeId.FaviconIco;
                else if (path.ToString().StartsWith("/files"))
                    webLogTypeId = Db.WebLogTypeId.TusFiles;
                else if (path.ToString().StartsWith("/downloads"))
                    webLogTypeId = Db.WebLogTypeId.Downloads;

                await Db.InsertWebLog(connectionString,
                    webLogTypeId,
                    ipAddress,
                    verb,
                    path.ToString(),
                    queryString,
                    statusCode,
                    elapsedTimeInMs,
                    refererS,
                    userAgent,
                    httpVersion,
                    loginId,
                    email,
                    roleName
                );


            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in Startup Web Log");
                // swallow
            }
        });


        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // 500 exceptions
            app.UseExceptionHandler("/error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // nginx is handling https redirection
        //app.UseHttpsRedirection();

        app.UseStaticFiles();

        // https://khalidabuhakmeh.com/handle-http-status-codes-with-razor-pages
        // https://andrewlock.net/retrieving-the-path-that-generated-an-error-with-the-statuscodepages-middleware/
        // >= 400 and < 600
        app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });
    }
}
