using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace Jumoo.Simpily.AuthSetup
{
    public partial class SimplyAuthSetup : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if ( !IsPostBack )
            {
                var membersetup = new MembershipSetup();
                lbMembersetup.Text = membersetup.CreateMembershipAttributes();

                // put all the content in the root, inside adrop down.
                var rootNodes = ApplicationContext.Current.Services.ContentService.GetRootContent().ToList();
                ddRootContent.DataSource = rootNodes;
                ddRootContent.DataTextField = "Name";
                ddRootContent.DataValueField = "Id";
                ddRootContent.DataBind();
            }
            else
            {
                var contentId = ddRootContent.SelectedValue;
                int rootId = 0;
                if ( !string.IsNullOrWhiteSpace(contentId) && int.TryParse(contentId, out rootId))
                {
                    lbContentSetup.Text = CreateRootContent(rootId);
                }
            }
        }

        private string CreateRootContent(int rootId)
        {
            StringBuilder contentStatus = new StringBuilder();
            var _cs = ApplicationContext.Current.Services.ContentService;
            var _cts = ApplicationContext.Current.Services.ContentTypeService;

            var rootNode =  _cs.GetById(rootId);
            if (rootNode == null)
            {
                contentStatus.Append("<p>Can't find that node </p>");
                return contentStatus.ToString();
            }

            contentStatus.Append("<li>Creating Login page: ");
            if (CreateContentNode("login", "Simpleauthlogin", rootNode, "login", "Please login", _cs))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Logout page: ");
            if (CreateContentNode("logout", "Simpleauthlogout", rootNode, "logout", "Logout", _cs))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Register page: ");
            if (CreateContentNode("register", "Simpleauthregister", rootNode, "Register", "Register for an account", _cs))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Reset page: ");
            if (CreateContentNode("reset", "Simpleauthreset", rootNode, "Reset your password", "Please login", _cs))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Verify page: ");
            if (CreateContentNode("verify", "Simpleauthverify", rootNode, "verify", "Verify your account", _cs))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<p><strong>Basic auth pages created</strong></p>");

            return contentStatus.ToString();
        }

        private bool CreateContentNode(string name, string contentType, IContent parent, string title, string body, IContentService _cs )
        {
            var exsitingNode = _cs.GetChildrenByName(parent.Id, name);
            if (exsitingNode.Any())
                return false;

            var node = _cs.CreateContentWithIdentity(name, parent, contentType);
            if (node != null)
            {
                node.SetValue("title", title);
                node.SetValue("bodyText", body);
                _cs.SaveAndPublishWithStatus(node);

                return true;
            }
            return false;
        }

        protected void btnCreateRootContent_Click(object sender, EventArgs e)
        {

        }
    }

    public class MembershipSetup
    {
        public MembershipSetup()
        {

        }

        /// <summary>
        ///  sets up the membership attributes for auth
        /// </summary>
        public string CreateMembershipAttributes()
        {
            LogHelper.Info<MembershipSetup>("Setting up Membership for SimpilyAuth");

            StringBuilder status = new StringBuilder();
            status.Append("<h3>Updating Default Member properties</h3>");

            var memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            var dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            var defaultMember = memberTypeService.Get("Member");

            if ( defaultMember == null )
            {
                status.Append("<strong>Can't find the standard 'Member' type");
                return status.ToString();
            }

            var textstring = dataTypeService.GetDataTypeDefinitionById(-88);
            var truefalse = dataTypeService.GetDataTypeDefinitionById(-49);

            status.Append("<ul>");

            if ( !defaultMember.PropertyTypeExists("resetGuid"))
            {
                LogHelper.Info<MembershipSetup>("Adding ResetGuid");
                status.Append("<li>Adding resetGuid</li>");
                
                defaultMember.AddPropertyType(new PropertyType(textstring)
                {
                    Alias = "resetGuid",
                    Name = "Reset Guid",
                    SortOrder = 0, 
                    Description = "Guid set when user requests a password reset"
                });
            }
            if ( !defaultMember.PropertyTypeExists("hasVerifiedAccount"))
            {
                LogHelper.Info<MembershipSetup>("Adding hasVerifiedAccount");
                status.Append("<li>Adding hasVerifiedAccount</li>");
                defaultMember.AddPropertyType(new PropertyType(truefalse)
                {
                    Alias = "hasVerifiedAccount",
                    Name = "Account Verified",
                    SortOrder = 0,
                    Description = "user has verified the account by email"
                });
            }
            if ( !defaultMember.PropertyTypeExists("joinedDate"))
            {
                LogHelper.Info<MembershipSetup>("Adding joinedDate");
                status.Append("<li>Adding joinedDate</li>");
                defaultMember.AddPropertyType(new PropertyType(textstring)
                {
                    Alias = "joinedDate",
                    Name = "Date Joined",
                    SortOrder = 0,
                    Description = "Date the user joined (verified their account)"
                });
            }

            status.Append("</ul>");
            status.Append("<p>Saving changes to membership</p>");

            LogHelper.Info<MembershipSetup>("Saving changed to memberTypeService");
            memberTypeService.Save(defaultMember);

            return status.ToString();
        }
    }
}