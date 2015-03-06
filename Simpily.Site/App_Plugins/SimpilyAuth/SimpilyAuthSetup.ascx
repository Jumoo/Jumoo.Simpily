<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SimpilyAuthSetup.ascx.cs" Inherits="App_Plugins_SimpilyAuth_SimpilyAuthSetup" %>
<div class="row">
    <div class="span12">
        <div class="page-header">
            <h3>Simpily Auth - Setup</h3>
        </div>
        <p>
            Setup will now attempt to setup the membership, and content.
        </p>
    </div>
</div>
<div class="row">
    <div class="span12">
        <asp:Label ID="lbMembersetup" runat="server"></asp:Label>
    </div>
</div>

<div class="row">
    <div class="span12">
        <div class="page-header">
            <h3>Authentication Pages</h3>
        </div>
        <p>
            The package is now installed, but you will need to create the webpages
            for login, registration and password reset, and account verification. 
        </p>
        <p>
            If you pick the relevant root content node from the list below, we can 
            create the pages for you (assuming you don't already have some).
        </p>
    </div>
</div>
<div class="row">
    <div class="span12">
        <asp:DropDownList ID="ddRootContent" runat="server"></asp:DropDownList>
        <asp:Button ID="btnCreateRootContent" runat="server" Text="Create Login Pages" OnClick="btnCreateRootContent_Click"  CssClass="btn btn-lg btn-primary"/>
    </div>
    <div class="span12">
        <asp:Label ID="lbContentSetup" runat="server"></asp:Label>
    </div>
</div>
