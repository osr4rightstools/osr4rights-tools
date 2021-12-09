using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using OSR4Rights.Web.BackgroundServices;
using Serilog;
using tusdotnet;
using tusdotnet.Helpers;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Concatenation;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;

namespace OSR4Rights.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            Configuration = configuration;
        }
        private readonly IHostEnvironment _hostEnvironment;
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSingleton(CreateTusConfiguration);
            services.AddHostedService<TusExpiredFilesCleanupService>();

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

            // face-search-go.cs writes to this channel
            // the channel that the background service will use.
            services.AddSingleton<FaceSearchFileProcessingChannel>();
            services.AddHostedService<FaceSearchFileProcessingService>();
            services.AddHostedService<FaceSearchCleanUpAzureService>();

            // hate-speech
            services.AddSingleton<HateSpeechFileProcessingChannel>();
            services.AddHostedService<HateSpeechFileProcessingService>();
            // TOOD - turn this back on!!!
            services.AddHostedService<HateSpeechCleanUpAzureService>();

            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Middleware
            // set request size limit for tus
            // log all requests for custom logging
            app.Use(async (context, next) =>
            {
                // Default limit was changed some time ago. Should work by setting MaxRequestBodySize to null using ConfigureKestrel but this does not seem to work for IISExpress.
                // Source: https://github.com/aspnet/Announcements/issues/267
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;

                // how long the request takes
                var watch = Stopwatch.StartNew();

                await next.Invoke();

                // This is the last in the pipeline

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

            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
                .WithExposedHeaders(CorsHelper.GetExposedHeaders()));

            // https://khalidabuhakmeh.com/handle-http-status-codes-with-razor-pages
            // https://andrewlock.net/retrieving-the-path-that-generated-an-error-with-the-statuscodepages-middleware/
            // >= 400 and < 600
            app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");

            app.UseSerilogRequestLogging();

            // TusDotNet
            // httpContext parameter can be used to create a tus configuration based on current user, domain, host, port or whatever.
            // In this case we just return the same configuration for everyone.
            app.UseTus(httpContext => Task.FromResult(httpContext.RequestServices.GetService<DefaultTusConfiguration>()));

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private DefaultTusConfiguration CreateTusConfiguration(IServiceProvider serviceProvider)
        {
            var tusFileStorePath = AppConfiguration.LoadFromEnvironment().TusFileStorePath;
            Log.Information($"tusFileStorePath is {tusFileStorePath}");
            try
            {
                if (Directory.Exists(tusFileStorePath))
                {
                    Log.Information("Directory already exists so don't need to create");
                }
                else
                {
                    Log.Information("Trying to create directory");
                    Directory.CreateDirectory(tusFileStorePath);
                }
            }
            catch (Exception ex)
            {
                // don't have perms to create on prod, so will do it in bash create scripts
                Log.Warning(ex, "Tried to create directory and failed");
            }

            var osrFileStorePath = AppConfiguration.LoadFromEnvironment().OsrFileStorePath;
            Log.Information($"osrFileStorePath is {osrFileStorePath}");
            try
            {
                if (Directory.Exists(osrFileStorePath))
                {
                    Log.Information("Directory already exists so don't need to create");
                }
                else
                {
                    Log.Information("Trying to create directory");
                    Directory.CreateDirectory(osrFileStorePath);
                }
            }
            catch (Exception ex)
            {
                // don't have perms to create on prod, so will do it in bash create scripts
                Log.Warning(ex, "Tried to create directory and failed");
            }


            return new DefaultTusConfiguration
            {
                // on what path should we listen for uploads?
                UrlPath = "/files",
                Store = new TusDiskStore(tusFileStorePath),
                MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
                Events = new Events
                {
                    OnAuthorizeAsync = ctx =>
                    {
                        // https://github.com/tusdotnet/tusdotnet/issues/140
                        var foo2 = ctx.HttpContext.AuthenticateAsync().Result;

                        // User has to be authenticated to upload files to tus via /files
                        if (!foo2.Principal.Identity.IsAuthenticated)
                        {
                            //ctx.HttpContext.Response.Headers.Add("WWW-Authenticate", new StringValues("Basic realm=tusdotnet-test-netcoreapp2.2"));
                            ctx.FailRequest(HttpStatusCode.Unauthorized);
                            return Task.CompletedTask;
                        }

                        // does the user has Role Claim Tier2 or Admin?
                        var shouldContinue = false;
                        var claims = foo2.Principal.Claims;

                        foreach (var claim in claims)
                        {
                            //if (claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            if (claim.Type == ClaimTypes.Role)
                            {
                                if (claim.Value == "Tier2" || claim.Value == "Admin")
                                {
                                    shouldContinue = true;
                                }
                            }
                        }

                        if (!shouldContinue)
                        {
                            Log.Warning("Role not in Tier2 or Admin trying to upload a file");
                            ctx.FailRequest(HttpStatusCode.Unauthorized);
                        }

                        //// Verify different things depending on the intent of the request.
                        //// E.g.:
                        ////   Does the file about to be written belong to this user?
                        ////   Is the current user allowed to create new files or have they reached their quota?
                        ////   etc etc
                        //switch (ctx.Intent)
                        //{
                        //    case IntentType.CreateFile:
                        //        break;
                        //    case IntentType.ConcatenateFiles:
                        //        break;
                        //    case IntentType.WriteFile:
                        //        break;
                        //    case IntentType.DeleteFile:
                        //        break;
                        //    case IntentType.GetFileInfo:
                        //        break;
                        //    case IntentType.GetOptions:
                        //        break;
                        //    default:
                        //        break;
                        //}

                        return Task.CompletedTask;
                    },

                    OnBeforeCreateAsync = ctx =>
                    {
                        // Partial files are not complete so we do not need to validate
                        // the metadata in our example.
                        if (ctx.FileConcatenation is FileConcatPartial)
                        {
                            return Task.CompletedTask;
                        }

                        //if (!ctx.Metadata.ContainsKey("name") || ctx.Metadata["name"].HasEmptyValue)
                        //{
                        //    ctx.FailRequest("name metadata must be specified. ");
                        //}

                        //if (!ctx.Metadata.ContainsKey("contentType") || ctx.Metadata["contentType"].HasEmptyValue)
                        //{
                        //    ctx.FailRequest("contentType metadata must be specified. ");
                        //}

                        // change name to filename
                        // change contentType to filetype

                        // okay can get the orig filename here
                        if (!ctx.Metadata.ContainsKey("filename") || ctx.Metadata["filename"].HasEmptyValue)
                        {
                            ctx.FailRequest("filename metadata must be specified. ");
                        }

                        if (!ctx.Metadata.ContainsKey("filetype") || ctx.Metadata["filetype"].HasEmptyValue)
                        {
                            ctx.FailRequest("filetype metadata must be specified. ");
                        }

                        return Task.CompletedTask;
                    },
                    OnCreateCompleteAsync = ctx =>
                    {
                        Log.Information($"Created file {ctx.FileId} using {ctx.Store.GetType().FullName}");
                        return Task.CompletedTask;
                    },
                    OnBeforeDeleteAsync = ctx =>
                    {
                        // Can the file be deleted? If not call ctx.FailRequest(<message>);
                        return Task.CompletedTask;
                    },
                    OnDeleteCompleteAsync = ctx =>
                    {
                        Log.Information($"Deleted file {ctx.FileId} using {ctx.Store.GetType().FullName}");
                        return Task.CompletedTask;
                    },

                    OnFileCompleteAsync = async ctx =>
                    {
                        Log.Information($"Upload of {ctx.FileId} completed using {ctx.Store.GetType().FullName}");
                        // If the store implements ITusReadableStore one could access the completed file here.
                        // The default TusDiskStore implements this interface:
                        //var file = await ctx.GetFileAsync();

                        // eventContext.FileId is the id of the file that was uploaded.
                        // eventContext.Store is the data store that was used (in this case an instance of the TusDiskStore)

                        // A normal use case here would be to read the file and do some processing on it.
                        ITusFile file = await ctx.GetFileAsync();
                        Log.Information($"File finished uploading {file.Id}");

                        // todo - do I need this in?
                        //return Task.CompletedTask;
                    }

                },

                // Set an expiration time where incomplete files can no longer be updated.
                // This value can either be absolute or sliding.
                // Absolute expiration will be saved per file on create
                // Sliding expiration will be saved per file on create and updated on each patch/update.

                //Expiration = new SlidingExpiration(TimeSpan.FromMinutes(5))

                // Browser stores data in LocalStorage too
                Expiration = new AbsoluteExpiration(TimeSpan.FromDays(1))
            };
        }
    }
}
