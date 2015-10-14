using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for SimplyAuth
/// </summary>
namespace SimpilyAuth
{
    /// <summary>
    ///  constants for view names and keys, to reduce my fat fingers.
    ///  
    ///  tweak this stuff, if you want to have things in diffrent places, or
    ///  use diffrent views for things.
    /// </summary>
    public static class SimpilyAuth
    {
        public const string ForgotPasswordView = "SimpilyAuth/SimpilyAuth.ForgotPassword";
        public const string ForgotPasswordKey = "ForgotPasswordForm";

        public const string LoginView = "SimpilyAuth/SimpilyAuth.Login";
        public const string LoginKey = "LoginForm";

        public const string ResetPasswordView = "SimpilyAuth/SimpilyAuth.ResetPassword";
        public const string ResetPasswordKey = "ResetPasswordForm";

        public const string RegisterView = "SimpilyAuth/SimpilyAuth.Register";
        public const string RegisterKey = "RegisterForm";

        /// <summary>
        ///  properties on the member ....
        /// </summary>

        public const string ResetRequestGuidPropery = "resetGuid";
        public const string AccountVerifiedProperty = "hasVerifiedAccount";
        public const string AccountJoinedDateProperty = "joinedDate";
        // public const string MemberDisplayNameProperty = "displayName";

        public const string NewAccountMemberType = "Member";

        public const string LoginUrl = "/login/";
        public const string ResetUrl = "/reset/";
        public const string RegisterUrl = "/register/";
        public const string VerifyUrl = "/verify/";

    }
}