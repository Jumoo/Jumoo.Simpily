using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;

using System.Net.Mail;
using System.Text;

/// <summary>
/// does the sending of email, for the SimpleAuth
/// assumes your umbraco install can already send email.
/// </summary>
public class EmailHelper
{

    public void SendResetPassword(string email, string guid)
    {
        try
        {
            LogHelper.Info<EmailHelper>("Send Reset: {0} {1}", () => email, () => guid);

            string from = UmbracoConfig.For.UmbracoSettings().Content.NotificationEmailAddress;

            MailMessage message = new MailMessage(from, email);
            message.Subject = "Reset your password";

            string baseURL = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            var resetUrl = baseURL + SimpleAuth.SimpleAuth.ResetUrl + "?resetGUID=" + guid;

            var messageBody = new StringBuilder();

            messageBody.Append("<h2>Password Reset</h2>");
            messageBody.Append("<p>we have received a request to reset your password</p>");
            messageBody.AppendFormat("<p><a href=\"{0}\">Reset your password</a></p>", resetUrl);

            message.IsBodyHtml = true;
            message.Body = messageBody.ToString();

            SmtpClient client = new SmtpClient();
            client.Send(message);
        }
        catch (Exception ex)
        {
            LogHelper.Info<EmailHelper>("Problems sending reset password email: {0}", () => ex.ToString());
        }
    }

    public void SendVerifyAccount(string email, string guid)
    {
        try
        {
            LogHelper.Info<EmailHelper>("Send Verify: {0} {1}", () => email, () => guid);

            string from = UmbracoConfig.For.UmbracoSettings().Content.NotificationEmailAddress;


            MailMessage message = new MailMessage(from, email);
            message.Subject = "Verifiy your account";

            string baseURL = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            var resetUrl = baseURL + SimpleAuth.SimpleAuth.VerifyUrl + "?verifyGUID=" + guid;

            var messageBody = new StringBuilder();

            messageBody.Append("<h2>Verifiy your account</h2>");
            messageBody.Append("<p>inorder to use your account, you first need to verifiy your email address</p>");
            messageBody.AppendFormat("<p><a href=\"{0}\">Verify your account</a>", resetUrl);

            message.IsBodyHtml = true;
            message.Body = messageBody.ToString();

            SmtpClient client = new SmtpClient();
            client.Send(message);
        }
        catch (Exception ex)
        {
            LogHelper.Info<EmailHelper>("Problems sending verify email: {0}", () => ex.ToString());
        }
                
    }
}