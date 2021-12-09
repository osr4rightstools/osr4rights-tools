using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using PostmarkDotNet;
using Serilog;

namespace OSR4Rights.Web
{
    public static class Email
    {
        public static async Task<bool> SendTemplate(string templateName, string toEmailAddress, string dataToSendUser, string postmarkServerToken)
        {

            var folder = "email-templates";
            var htmlTop = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-top.html"));
            var htmlBottom = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-bottom.html"));
            string subject;
            string htmlBody;
            string textBody;

            if (templateName == "register")
            {
                // html
                var htmlMiddle = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-register.html"));
                htmlMiddle = htmlMiddle.Replace("{{guid}}", dataToSendUser);
                htmlBody = htmlTop + htmlMiddle + htmlBottom;

                // text
                textBody = await File.ReadAllTextAsync(Path.Combine(folder, "text-register.html"));
                textBody = textBody.Replace("{{guid}}", dataToSendUser);

                subject = "Confirm email address for registration";
            }
            else if (templateName == "forgot-password")
            {
                // html
                var htmlMiddle = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-forgot-password.html"));
                htmlMiddle = htmlMiddle.Replace("{{guid}}", dataToSendUser);
                htmlBody = htmlTop + htmlMiddle + htmlBottom;

                // text
                textBody = await File.ReadAllTextAsync(Path.Combine(folder, "text-forgot-password.html"));
                textBody = textBody.Replace("{{guid}}", dataToSendUser);

                subject = "Forgot Password";
            }
            else if (templateName == "face-search-job-complete")
            {
                // html
                var htmlMiddle = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-face-search-job-complete.html"));
                htmlMiddle = htmlMiddle.Replace("{{jobId}}", dataToSendUser);
                htmlBody = htmlTop + htmlMiddle + htmlBottom;

                // text
                textBody = await File.ReadAllTextAsync(Path.Combine(folder, "text-face-search-job-complete.html"));
                textBody = textBody.Replace("{{jobId}}", dataToSendUser);

                subject = "FaceSearch Job Complete";
            }
            else if (templateName == "hate-speech-job-complete")
            {
                // html
                var htmlMiddle = await File.ReadAllTextAsync(Path.Combine(folder, "html-template-hate-speech-job-complete.html"));
                htmlMiddle = htmlMiddle.Replace("{{jobId}}", dataToSendUser);
                htmlBody = htmlTop + htmlMiddle + htmlBottom;

                // text
                textBody = await File.ReadAllTextAsync(Path.Combine(folder, "text-hate-speech-job-complete.html"));
                textBody = textBody.Replace("{{jobId}}", dataToSendUser);

                subject = "HateSpeech Job Complete";
            }
            else
            {
                Log.Warning($"Unrecognised template name passed of {templateName}");
                return false;
            }

            Log.Information($"email sending to {toEmailAddress}, subject {subject}, textBody is {textBody}");

            // Send email via PostMark
            var client = new PostmarkClient(postmarkServerToken);
            var postmarkMessage = new PostmarkMessage
            {
                To = toEmailAddress,
                From = "help@osr4rightstools.org", // has to be a Sender Signature on postmark account
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            try
            {
                var sendResult = await client.SendMessageAsync(postmarkMessage);

                if (sendResult is { Status: PostmarkStatus.Success })
                {
                    Log.Information("send email success");
                    return true;
                }
                Log.Warning($"Send fail Postmark {sendResult?.Status} {sendResult?.Message}");
            }
            catch (Exception ex)
            {
                // Calls to the client can throw an exception 
                // if the request to the API times out.
                // or if the From address is not a Sender Signature 
                Log.Error(ex, $"{nameof(Email)} helper method - sending mail via Postmark error");
                return false;
            }

            // very unusual to get here - exception in catch block
            return false;
        }

        // No templating - used for messages to admins, and not to end users
        public static async Task<bool> Send(OSREmail osrEmail, string postmarkServerToken, string gmailPassword)
        {
            var sendViaGmail = false;

            // emails will only be sent to davemateer@gmail.com 
            //var inTestingMode = false;
            //var inTestingMode = true;

            if (string.IsNullOrWhiteSpace(postmarkServerToken)) throw new ArgumentNullException(nameof(postmarkServerToken));
            if (string.IsNullOrWhiteSpace(gmailPassword)) throw new ArgumentNullException(nameof(gmailPassword));
            if (osrEmail == null) throw new ArgumentNullException(nameof(osrEmail));

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

                        <!-- contact us link was here -->

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

            // read template from file so don't have to worry about double escaping ""

            //<p style=""font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0;""><a href=""https://osr4rights.org/contact-us/"">Contact Us</a> with any questions. Thank you for using OSR4Rights.</p>
            string? textBodyTesting = null;
            string? htmlBodyTesting = null;

            var toEmailAddress = osrEmail.ToEmailAddress;

            // Testing mode
            //if (inTestingMode)
            //{
            //    toEmailAddress = "davemateer@gmail.com";

            //    Log.Information($"toEmailAddress: {toEmailAddress} but should be for: {osrEmail.ToEmailAddress}");

            //    textBodyTesting = $"** Testing Mode. Email should be sent to: {osrEmail.ToEmailAddress} **";
            //    htmlBodyTesting = $"<p>** Testing Mode. Email should be sent to: {osrEmail.ToEmailAddress} **</p>";
            //}
            //else
            //    Log.Information($"toEmailAddress: {toEmailAddress}");

            // Replace in template
            template = template.Replace("{{htmltext}}", osrEmail.HtmlBody);


            var textBody = osrEmail.TextBody + "Kind Regards, OSR4Rights Team" + textBodyTesting;
            var htmlBody = template + htmlBodyTesting;

            // hack
            //htmlBody = template2;

            Log.Information($"Subject: {osrEmail.Subject}");
            Log.Information($"TextBody {textBody}");
            Log.Debug($"HtmlBody {htmlBody}");

            if (sendViaGmail)
            {
                // comment back in to send via gmail
                //var m = new MimeMessage();

                //var fromAddress = "dave@osr4rightstools.org";
                //m.From.Add(new MailboxAddress("Dave Mateer", fromAddress));
                ////m.To.Add(new MailboxAddress("Dave (Gmail)", toEmailAddress));
                //// todo a to name?
                //m.To.Add(MailboxAddress.Parse(toEmailAddress));

                //m.Subject = osrEmail.Subject;

                //var bodyBuilder = new BodyBuilder
                //{
                //    HtmlBody = htmlBody,
                //    TextBody = textBody
                //};

                //m.Body = bodyBuilder.ToMessageBody();
                //try
                //{
                //    using var c = new SmtpClient();
                //    await c.ConnectAsync("smtp.gmail.com", 587);
                //    // Note: since we don't have an OAuth2 token, disable
                //    // the XOAUTH2 authentication mechanism.
                //    c.AuthenticationMechanisms.Remove("XOAUTH2");

                //    // Note: only needed if the SMTP server requires authentication
                //    await c.AuthenticateAsync("dave@osr4rightstools.org", gmailPassword);
                //    await c.SendAsync(m);
                //    await c.DisconnectAsync(true);

                //    return true;
                //}
                //catch (Exception ex)
                //{
                //    Log.Error(ex, "Gmail Exception sending email - need to investigate! Dumping out salient points of email next into Error log for manual resend");

                //    Log.Error($"Trying to send email to final address of {osrEmail.ToEmailAddress}");
                //    Log.Error($"Subject {osrEmail.Subject}");
                //    Log.Error($"With body {osrEmail.HtmlBody}");
                //    return false;
                //}
                return false;
            }
            else
            {
                // Send email via PostMark
                var client = new PostmarkClient(postmarkServerToken);
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

                //PostmarkResponse? sendResult = null;
                try
                {
                    var sendResult = await client.SendMessageAsync(postmarkMessage);

                    if (sendResult is { Status: PostmarkStatus.Success })
                    {
                        Log.Information("send email success");
                        return true;
                        //return sendResult;
                    }
                    Log.Warning($"Send fail Postmark {sendResult?.Status} {sendResult?.Message}");
                }
                catch (Exception ex)
                {
                    // Calls to the client can throw an exception 
                    // if the request to the API times out.
                    // or if the From address is not a Sender Signature 
                    Log.Error(ex, $"{nameof(Email)} helper method - sending mail via Postmark error");
                    return false;
                    //return sendResult;
                }

                //return sendResult;
                // unlikely to every get here (only if catch block throws)
                return false;
            }
        }

    }
}
