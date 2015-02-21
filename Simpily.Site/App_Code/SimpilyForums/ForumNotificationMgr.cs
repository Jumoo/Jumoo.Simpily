using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;

using Jumoo.Simpily;

using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Security;
using System.Net.Mail;
using System.Text;
using Umbraco.Core.Configuration;
using System.Text.RegularExpressions;

/// <summary>
/// Notification Manager for Simpily Forums
/// 
/// sends notification emails, when someone posts in a forum, also
/// gives an example of how to hook into the Simpily Forum events.
/// 
/// you need mail settings already setup in your umbraco site
/// </summary>
public class ForumNotificationMgr : ApplicationEventHandler 
{
    protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
    {
        SimpilyForumsController.OnPostSaved += SimpilyForumsController_OnPostSaved;
    }

    void SimpilyForumsController_OnPostSaved(IContent sender, SimpilyForumsEventArgs e)
    {
        // we get passed IContent - but as we are going to go get loads of 
        // other content, we will be quicker using IPublishedContent 
        var umbHelper = new UmbracoHelper(UmbracoContext.Current);
        var mbrHelper = new MembershipHelper(UmbracoContext.Current);

        var post = umbHelper.TypedContent(sender.Id);
        if (post == null)
            return;

        // for us - the current user will have been the author
        var author = mbrHelper.GetCurrentMember();

        // work out the root of this post (top of the thread)
        var postRoot = post;
        if (post.Parent.DocumentTypeAlias == "Simpilypost")
        {
            // if we have a parent post, then this is a reply 
            postRoot = post.Parent;
        }

        LogHelper.Info<ForumNotificationMgr>("Sending Notification for new post for {0}", () => postRoot.Name);

        List<string> receipients = GetRecipients(postRoot, mbrHelper);
        
        // remove the author from the list.
        var postAuthor = GetAuthorEmail(post, mbrHelper);
        if (receipients.Contains(postAuthor))
            receipients.Remove(postAuthor);
        
        if (receipients.Any())
        {
            SendNotificationEmail(postRoot, post, author, receipients, e.NewPost);
        }
    }

    List<string> GetRecipients(IPublishedContent item, MembershipHelper mbrHelper)
    {
        List<string> recipients = new List<string>();

        foreach(var childPost in item.Children().Where(x => x.IsVisible()))
        {
            var postAuthorEmail = GetAuthorEmail(childPost, mbrHelper);
            if (!string.IsNullOrWhiteSpace(postAuthorEmail) && !recipients.Contains(postAuthorEmail))
            {
                LogHelper.Info<ForumNotificationMgr>("Adding: {0}", () => postAuthorEmail);
                recipients.Add(postAuthorEmail);
            }
        }
        return recipients;
    }

    string GetAuthorEmail(IPublishedContent post, MembershipHelper mbrHelper)
    {
        if (post == null)
            return string.Empty;

        var authorId = post.GetPropertyValue<int>("postAuthor", 0);
        if ( authorId > 0)
        {
            var author = mbrHelper.GetById(authorId);
            if ( author != null)
            {
                // pre 7.2.2 - you can't do get propertyvalue to get
                // system values like Email 
                return author.AsDynamic().Email;
            }
        }

        return string.Empty;
    }

    void SendNotificationEmail(IPublishedContent root, IPublishedContent post, IPublishedContent author, List<string>recipients, bool newPost)
    {
        var threadTitle = root.GetPropertyValue<string>("postTitle", root.Name);
        var updateBody = post.GetPropertyValue<HtmlString>("postBody");
        string fromAddress = UmbracoConfig.For.UmbracoSettings().Content.NotificationEmailAddress;

        var authorName = "Someone";
        if (author != null)
            authorName = author.Name;

        // smtp (assuming you've set all this up)
        SmtpClient smtp = new SmtpClient();

        MailMessage message = new MailMessage();
        message.From = new MailAddress(fromAddress);     

        string siteUrl = HttpContext.Current.Request.Url.AbsoluteUri
            .Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
        string postUrl = string.Format("{0}{1}", siteUrl, root.Url);

        // build the subject and body up
        var subjectTemplate = "{{newOld}} comment on [{{postTitle}}]";
        message.Subject = GetEmailTemplate(subjectTemplate, "SimpilyForums.NotificationSubject",
                            threadTitle, updateBody.ToString(), authorName, postUrl, newPost);

        string bodyTemplate = "<p>{{author}} has posted a comment on {{postTitle}}</p>" +
            "<div style=\"border-left: 4px solid #444;padding:0.5em;font-size:1.3em\">{{body}}</div>" +
            "<p>you can view all the comments here: <a href=\"{{threadUrl}}\">{{threadUrl}}</a>";

        message.Body = GetEmailTemplate(bodyTemplate, "SimpiyForums.NotificationBody",
                            threadTitle, updateBody.ToString(), authorName, postUrl, newPost);
        
        message.IsBodyHtml = true;

        foreach(var recipient in recipients)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
                message.Bcc.Add(recipient);
        }

        try
        {
            LogHelper.Info<ForumNotificationMgr>("Sending Email {0} to {1} people", () => threadTitle, () => recipients.Count);
            smtp.Send(message);
        }
        catch (Exception ex)
        {
            LogHelper.Info<ForumNotificationMgr>("Failed to send the email - probibly because email isn't configured for this site\n {0}", ()=> ex.ToString());
        }
    }

    string GetEmailTemplate(string template, string dictionaryString, string postTitle, string body, string author, string threadUrl, bool newPost)
    {
        var umbHelper = new UmbracoHelper(UmbracoContext.Current);

        var dictionaryTemplate = umbHelper.GetDictionaryValue(dictionaryString);
        if ( !string.IsNullOrWhiteSpace(dictionaryTemplate)) {
            template = dictionaryTemplate;
        }

        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("{{author}}", author);
        parameters.Add("{{postTitle}}", postTitle);
        parameters.Add("{{body}}", body);
        parameters.Add("{{threadUrl}}", threadUrl);
        parameters.Add("{{newOld}}", newPost ? "New" : "Updated");

        return template.ReplaceMany(parameters);;
    }
    
}