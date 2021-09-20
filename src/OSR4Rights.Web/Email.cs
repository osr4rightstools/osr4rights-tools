using System;
using System.Threading.Tasks;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web
{
    public static class Email
    {
        public static async Task<PostmarkResponse?> Send(string postmarkServerToken, OSREmail osrEmail)
        {
            // emails will only be send to davemateer@gmail.com 
            var inTestingMode = false;
            //var inTestingMode = true;

            if (string.IsNullOrWhiteSpace(postmarkServerToken)) throw new ArgumentNullException(nameof(postmarkServerToken));

            if (osrEmail == null) throw new ArgumentNullException(nameof(osrEmail));

            var client = new PostmarkClient(postmarkServerToken);

            // notice double ""
            // which is just an escaped "
            // https://github.com/leemunroe/responsive-html-email-template/
            var template = @"
<!doctype html>
<html>
  <head>
    <meta name=""viewport"" content=""width=device-width"">
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <title>OSR4Rights Tools</title>
    <style>
    /* -------------------------------------
        INLINED WITH htmlemail.io/inline
    ------------------------------------- */
    /* -------------------------------------
        RESPONSIVE AND MOBILE FRIENDLY STYLES
    ------------------------------------- */
    @media only screen and (max-width: 620px) {
      table[class=body] h1 {
        font-size: 28px !important;
        margin-bottom: 10px !important;
      }
      table[class=body] p,
            table[class=body] ul,
            table[class=body] ol,
            table[class=body] td,
            table[class=body] span,
            table[class=body] a {
        font-size: 16px !important;
      }
      table[class=body] .wrapper,
            table[class=body] .article {
        padding: 10px !important;
      }
      table[class=body] .content {
        padding: 0 !important;
      }
      table[class=body] .container {
        padding: 0 !important;
        width: 100% !important;
      }
      table[class=body] .main {
        border-left-width: 0 !important;
        border-radius: 0 !important;
        border-right-width: 0 !important;
      }
      table[class=body] .btn table {
        width: 100% !important;
      }
      table[class=body] .btn a {
        width: 100% !important;
      }
      table[class=body] .img-responsive {
        height: auto !important;
        max-width: 100% !important;
        width: auto !important;
      }
    }

    /* -------------------------------------
        PRESERVE THESE STYLES IN THE HEAD
    ------------------------------------- */
    @media all {
      .ExternalClass {
        width: 100%;
      }
      .ExternalClass,
            .ExternalClass p,
            .ExternalClass span,
            .ExternalClass font,
            .ExternalClass td,
            .ExternalClass div {
        line-height: 100%;
      }
      .apple-link a {
        color: inherit !important;
        font-family: inherit !important;
        font-size: inherit !important;
        font-weight: inherit !important;
        line-height: inherit !important;
        text-decoration: none !important;
      }
      #MessageViewBody a {
        color: inherit;
        text-decoration: none;
        font-size: inherit;
        font-family: inherit;
        font-weight: inherit;
        line-height: inherit;
      }
      .btn-primary table td:hover {
        background-color: #34495e !important;
      }
      .btn-primary a:hover {
        background-color: #34495e !important;
        border-color: #34495e !important;
      }
    }
    </style>
  </head>
  <body class="""" style=""background-color: #f6f6f6; font-family: sans-serif; -webkit-font-smoothing: antialiased; font-size: 14px; line-height: 1.4; margin: 0; padding: 0; -ms-text-size-adjust: 100%; -webkit-text-size-adjust: 100%;"">
    <span class=""preheader"" style=""color: transparent; display: none; height: 0; max-height: 0; max-width: 0; opacity: 0; overflow: hidden; mso-hide: all; visibility: hidden; width: 0;"">OSR4Rights</span>
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" class=""body"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%; background-color: #f6f6f6;"">
      <tr>
        <td style=""font-family: sans-serif; font-size: 14px; vertical-align: top;"">&nbsp;</td>
        <td class=""container"" style=""font-family: sans-serif; font-size: 14px; vertical-align: top; display: block; Margin: 0 auto; max-width: 580px; padding: 10px; width: 580px;"">
          <div class=""content"" style=""box-sizing: border-box; display: block; Margin: 0 auto; max-width: 580px; padding: 10px;"">

            <!-- START CENTERED WHITE CONTAINER -->
            <table class=""main"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%; background: #ffffff; border-radius: 3px;"">

              <!-- START MAIN CONTENT AREA -->
              <tr>
                <td class=""wrapper"" style=""font-family: sans-serif; font-size: 14px; vertical-align: top; box-sizing: border-box; padding: 20px;"">
                  <table border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;"">
                    <tr>
                      <td style=""font-family: sans-serif; font-size: 14px; vertical-align: top;"">

                        <p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0; Margin-bottom: 15px;"">{{htmltext}}</p>


                        <p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0;"">Kind Regards</p>
                        
                        <p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0; Margin-bottom: 15px;"">OSR4Rights Team.</p>

                        <p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0;""><a href=""https://osr4rights.org/contact-us/"">Contact Us</a> with any questions. Thank you for using OSR4Rights.</p>

                       <p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0; Margin-bottom: 15px;""></p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>

            <!-- END MAIN CONTENT AREA -->
            </table>

            <!-- START FOOTER -->
            <div class=""footer"" style=""clear: both; Margin-top: 10px; text-align: center; width: 100%;"">
              <table border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;"">
                <tr>
                  <td class=""content-block"" style=""font-family: sans-serif; vertical-align: top; padding-bottom: 10px; padding-top: 10px; font-size: 12px; color: #999999; text-align: center;"">
                    <span class=""apple-link"" style=""color: #999999; font-size: 12px; text-align: center;""HILLARY RODHAM CLINTON SCHOOL OF LAW, SWANSEA UNIVERSITY, SINGLETON PARK, SWANSEA. SA2 8PP</span>
                    <br><a href=""https://osr4rightstools.org"" style=""text-decoration: underline; color: #999999; font-size: 12px; text-align: center;"">osr4rightstools.org</a> </td> </tr>
                <tr>
                  <td class=""content-block powered-by"" style=""font-family: sans-serif; vertical-align: top; padding-bottom: 10px; padding-top: 10px; font-size: 12px; color: #999999; text-align: center;"">
                    Powered by <a href=""http://htmlemail.io"" style=""color: #999999; font-size: 12px; text-align: center; text-decoration: none;"">HTMLemail</a>.
                  </td>
                </tr>
              </table>
            </div>
            <!-- END FOOTER -->

          <!-- END CENTERED WHITE CONTAINER -->
          </div>
        </td>
        <td style=""font-family: sans-serif; font-size: 14px; vertical-align: top;"">&nbsp;</td>
      </tr>
    </table>
  </body>
</html>
";

            string? textBodyTesting = null;
            string? htmlBodyTesting = null;

            var toEmailAddress = osrEmail.ToEmailAddress;

            if (inTestingMode)
            {
                toEmailAddress = "davemateer@gmail.com";

                Log.Information($"toEmailAddress: {toEmailAddress} but should be for: {osrEmail.ToEmailAddress}");

                textBodyTesting = $"** Testing Mode. Email should be sent to: {osrEmail.ToEmailAddress} **";
                htmlBodyTesting = $"<p>** Testing Mode. Email should be sent to: {osrEmail.ToEmailAddress} **</p>";
            }
            else
                Log.Information($"toEmailAddress: {toEmailAddress}");

            // Replace in template
            template = template.Replace("{{htmltext}}", osrEmail.HtmlBody);

            var textBody = osrEmail.TextBody + "Kind Regards, OSR4Rights Team" + textBodyTesting;
            var htmlBody = template + htmlBodyTesting;
            
            Log.Information($"Subject: {osrEmail.Subject}");
            Log.Information($"TextBody {textBody}");
            Log.Debug($"HtmlBody {htmlBody}");

            // Send email via PostMark
            var postmarkMessage = new PostmarkMessage
            {
                To = toEmailAddress,
                //From = "dave@hmsoftware.co.uk", // has to be a Sender Signature on postmark account
                From = "help@osr4rightstools.org", // has to be a Sender Signature on postmark account
                                                   //TrackOpens = true,
                Subject = osrEmail.Subject,
                TextBody = textBody,
                //HtmlBody = "<html><body><img src=\"cid:embed_name.jpg\"/></body></html>",
                HtmlBody = htmlBody
                //Tag = "business-message",
                //Headers = new HeaderCollection{
                //    {"X-CUSTOM-HEADER", "Header content"}
                //}
            };
            //var imageContent = System.IO.File.ReadAllBytes("test.jpg");
            //message.AddAttachment(imageContent, "test.jpg", "image/jpg", "cid:embed_name.jpg");

            PostmarkResponse? sendResult = null;
            try
            {
                sendResult = await client.SendMessageAsync(postmarkMessage);

                if (sendResult is { Status: PostmarkStatus.Success })
                {
                    Log.Information("send email success");
                    return sendResult;
                }
                Log.Warning($"Send fail Postmark {sendResult?.Status} {sendResult?.Message}");
            }
            catch (Exception ex)
            {
                // Calls to the client can throw an exception 
                // if the request to the API times out.
                // or if the From address is not a Sender Signature 
                Log.Error(ex, $"{nameof(Email)} helper method - sending mail via Postmark error");
                return sendResult;
            }

            return sendResult;
        }
    }
}
