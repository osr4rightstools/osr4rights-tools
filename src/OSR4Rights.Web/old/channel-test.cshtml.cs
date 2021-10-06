using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BackgroundServiceTest.BackgroundServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSR4Rights.Web.BackgroundServices;
using Serilog;

namespace OSR4Rights.Web.Pages
{

    public class ChannelTestModel : PageModel
    {
        private readonly Test2Channel _test2Channel;
        public ChannelTestModel(Test2Channel test2Channel)
        {
            _test2Channel = test2Channel;
        }

        public async Task OnGet()
        {
            var filename = DateTime.Now.ToLongTimeString();
            await _test2Channel.AddFileAsync(filename);
        }

        //private readonly FaceSearchFileProcessingChannel _boundedMessageChannel;
        //public int CountOfFileProcessingChannel { get; set; }

        //public ChannelTestModel(FaceSearchFileProcessingChannel boundedMessageChannel)
        //{
        //    _boundedMessageChannel = boundedMessageChannel;
        //}

        //public async Task OnGet()
        //{
        //    await _boundedMessageChannel.AddFileAsync("dummy.txt");

        //    var foo = _boundedMessageChannel.CountOfFileProcessingChannel();
        //    CountOfFileProcessingChannel = foo;
        //}



        //public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        //{
        //    // currently we just need some file to be uploaded
        //    if (ModelState.IsValid)
        //    {
        //        // check that Upload is something
        //        if (Upload is { } && Upload.Length > 0)
        //        {
        //            Log.Information($"Uploaded file {Upload.FileName}");

        //            // eg C:\Users\djhma\AppData\Local\Temp
        //            var path = Path.GetTempPath();

        //            var foo = DateTimeOffset.Now.ToUnixTimeSeconds();

        //            var fileName = Path.Combine(path, $"job-{foo}.tmp");

        //            // copy uploaded file to tempPath location as job-{jobId}.tmp
        //            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        //            {
        //                await Upload.CopyToAsync(fileStream, cancellationToken);
        //            }

        //            Log.Information($"Saved file {fileName}");

        //            return LocalRedirect($"/");
        //        }
        //    }

        //    return Page();
        //}
    }
}
