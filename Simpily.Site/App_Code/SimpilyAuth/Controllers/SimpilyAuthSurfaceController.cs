﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Umbraco.Web.Mvc;

using Umbraco.Web;
using Umbraco.Web.Security;

using Umbraco.Core.Models;
using Umbraco.Core.Logging;

namespace SimpilyAuth
{
    /// <summary>
    /// Summary description for SimpleAuthSurfaceController
    /// </summary>
    public class SimpilyAuthSurfaceController : SurfaceController
    {
        public ActionResult RenderLogin()
        {
            SimpilyLoginViewModel login = new SimpilyLoginViewModel();


            LogHelper.Info<SimpilyAuthSurfaceController>("Login URL: {0}", () => HttpContext.Request.Url.AbsolutePath);

            login.ReturnUrl = HttpContext.Request.Url.ToString() ;
            if ( HttpContext.Request.Url.AbsolutePath == SimpilyAuth.LoginUrl)
            {
                login.ReturnUrl = "/";
            }

            return PartialView(SimpilyAuth.LoginView, login);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleLogin(SimpilyLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var membershipHelper = new MembershipHelper(UmbracoContext.Current);

            if (membershipHelper.IsLoggedIn())
            {
                return RedirectToRoute(model.ReturnUrl);
            }

            // login in the user
            try
            {
                if (membershipHelper.Login(model.EmailAddress, model.Password))
                {
                    // logged in
                    var member = membershipHelper.GetByEmail(model.EmailAddress);


                    if (member != null)
                    {
                        if (member.GetPropertyValue<bool>(SimpilyAuth.AccountVerifiedProperty, false, false))
                        {
                            // a valid and verified user here be!
                            TempData["returnUrl"] = model.ReturnUrl;
                            return RedirectToCurrentUmbracoPage();
                        }
                        else
                        {
                            // we need to validate this account before they can logon.
                            ModelState.AddModelError(SimpilyAuth.LoginKey, 
                                GetDictionaryOrDefault("SimpleAuth.Login.NotVerified", "Email has not been verified"));

                            membershipHelper.Logout();
                            return CurrentUmbracoPage();
                            // return PartialView(SimpleAuth.LoginView, model);
                        }
                    }
                    else
                    {
                        // can't find the user...?
                        ModelState.AddModelError(SimpilyAuth.LoginKey, 
                            GetDictionaryOrDefault("SimpleAuth.Login.NoUser", "Invalid Details"));
                    }
                }
                else
                {
                    // can't login this person...
                    ModelState.AddModelError(SimpilyAuth.LoginKey, 
                        GetDictionaryOrDefault("SimpleAuth.Login.LoginFail","Invalid Details"));
                }

            }
            catch (Exception ex)
            {
                // somethig big time went boom...
                ModelState.AddModelError(SimpilyAuth.LoginKey, "Error logging on" + ex.ToString());
            }

            return CurrentUmbracoPage();
        }

        public ActionResult Logout()
        {
            var membershipHelper = new MembershipHelper(UmbracoContext.Current);

            if (membershipHelper.IsLoggedIn())
            {
                membershipHelper.Logout();
                return Content( GetDictionaryOrDefault("SimpleAuth.Logout.LoggedOut", "Logged out"));
            }
            else
            {
                return Content( GetDictionaryOrDefault("SimpleAuth.Logout.NotLoggedIn", "Wasn't logged in"));
            }
        }


        public ActionResult RenderForgotPassword()
        {
            return PartialView(SimpilyAuth.ForgotPasswordView, new SimpilyForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgotPassword(SimpilyForgotPasswordModel model)
        {
            TempData["ResetSent"] = false;
            if (!ModelState.IsValid)
            {
                return PartialView(SimpilyAuth.ForgotPasswordView, model);
            }

            // var membershipHelper = new MembershipHelper(UmbracoContext.Current);
            var memberService = ApplicationContext.Services.MemberService;

            // var member = membershipHelper.GetByEmail(model.EmailAddress);
            var member = memberService.GetByEmail(model.EmailAddress);

            if (member != null)
            {
                // we found a user with that email ....
                DateTime expires = DateTime.Now.AddMinutes(20);

                // member.GetPropertyValue("resetGuid") = expires.ToString("ddMMyyyyHmmssFFFF");

                member.SetValue(SimpilyAuth.ResetRequestGuidPropery, expires.ToString("ddMMyyyyHmmssFFFF"));
                memberService.Save(member);

                // send email....
                EmailHelper helper = new EmailHelper();
                helper.SendResetPassword(member.Email, expires.ToString("ddMMyyyyHmmssFFFF"));

                TempData["ResetSent"] = true;
            }
            else
            {
                ModelState.AddModelError(SimpilyAuth.ForgotPasswordKey, 
                    GetDictionaryOrDefault("SimpleAuth.Reset.NoUser", "No user found"));
                return PartialView(SimpilyAuth.ForgotPasswordView);
            }

            return CurrentUmbracoPage();
        }


        public ActionResult RenderResetPassword()
        {
            return PartialView(SimpilyAuth.ResetPasswordView, new SimpilyPasswordResetModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(SimpilyPasswordResetModel model)
        {
            TempData["Success"] = false;

            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var memberService = ApplicationContext.Services.MemberService;

            try
            {
                var member = memberService.GetByEmail(model.EmailAddress);

                if (member != null)
                {
                    var resetGuid = Request.QueryString["resetGUID"];

                    if (!string.IsNullOrWhiteSpace(resetGuid))
                    {
                        if (member.GetValue<string>(SimpilyAuth.ResetRequestGuidPropery) == resetGuid)
                        {
                            // ok so the match. check to see if it hasn't expired...

                            DateTime expiry = DateTime.ParseExact(resetGuid, "ddMMyyyyHHmmssFFFF", null);

                            // is expiry less than now.
                            if (DateTime.Now.CompareTo(expiry) < 0)
                            {
                                memberService.SavePassword(member, model.Password);

                                member.SetValue(SimpilyAuth.ResetRequestGuidPropery, string.Empty);
                                memberService.Save(member);

                                TempData["Success"] = true;
                                return CurrentUmbracoPage();
                            }
                            else
                            {
                                ModelState.AddModelError(SimpilyAuth.ResetPasswordKey,
                                    GetDictionaryOrDefault("SimpleAuth.Reset.Expired", "The reset request has expired"));
                                return CurrentUmbracoPage();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(SimpilyAuth.ResetPasswordKey, 
                                GetDictionaryOrDefault("SimpleAuth.Reset.NoRequest", "No reset request has been found"));
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(SimpilyAuth.ResetPasswordKey, 
                            GetDictionaryOrDefault("SimpleAuth.Reset.NoAccount", "Cannot find account"));
                    }
                }
                else
                {
                    ModelState.AddModelError(SimpilyAuth.ResetPasswordKey,
                        GetDictionaryOrDefault("SimpleAuth.Reset.NoAccount", "Cannot find account"));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(SimpilyAuth.ResetPasswordKey, "Error Resetting Password: " + ex.Message);
            }

            return CurrentUmbracoPage();
        }

        public ActionResult RenderRegister()
        {
            return PartialView(SimpilyAuth.RegisterView, new SimpilyRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleRegister(SimpilyRegisterViewModel model)
        {
            TempData["RegisterComplete"] = false;

            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var memberService = ApplicationContext.Services.MemberService;

            if ( EmailAddressExists(model.EmailAddress ))
            {
                ModelState.AddModelError(SimpilyAuth.RegisterKey,
                    GetDictionaryOrDefault("SimpleAuth.Register.ExistingEmail", "Email Address is already in use"));
                return CurrentUmbracoPage();
            }

            if ( !IsValidEmailDomain(model.EmailAddress ))
            {
                ModelState.AddModelError(SimpilyAuth.RegisterKey, 
                    GetDictionaryOrDefault("SimpleAuth.Register.InvalidDomain", "You cannot register for this site with that email address"));
                return CurrentUmbracoPage();
            }

            var memberTypeService = ApplicationContext.Services.MemberTypeService;
            var memberType = memberTypeService.Get(SimpilyAuth.NewAccountMemberType);

            try
            {
                var newMember = memberService.CreateMemberWithIdentity(
                    model.EmailAddress, model.EmailAddress, model.Name, memberType);

                memberService.SavePassword(newMember, model.Password);

                newMember.SetValue(SimpilyAuth.AccountVerifiedProperty, false);
                // newMember.SetValue("profileUrl", newMember.id);

                memberService.Save(newMember);

                // add new user to any groups ?
                var groupsToAdd = CurrentPage.GetPropertyValue<string>("defaultMembership");

                if (!string.IsNullOrWhiteSpace(groupsToAdd))
                {
                    var _mgs = ApplicationContext.Services.MemberGroupService;
                    var groups = groupsToAdd.Split(',');

                    foreach (var group in groups)
                    {
                        var memberGroup = _mgs.GetByName(group);
                        if (memberGroup != null)
                        {
                            LogHelper.Info<SimpilyAuthSurfaceController>("Adding user to group", () => group);
                            memberService.AssignRole(newMember.Id, group);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(SimpilyAuth.RegisterKey, "Error creating account:" + ex.ToString());
                return CurrentUmbracoPage();
            }

            // now do the verification-ness.
            var member = memberService.GetByEmail(model.EmailAddress);
            if (member != null)
            {
                member.SetValue(SimpilyAuth.AccountJoinedDateProperty, DateTime.Now.ToString("dd-MMM-yyyy @ HH:mm:ss"));
                memberService.Save(member);

                // send out the email (usethe user key as the guid)
                // member.Key
                EmailHelper helper = new EmailHelper();
                helper.SendVerifyAccount(model.EmailAddress, member.Key.ToString());
            }

            TempData["RegisterComplete"] = true;
            return CurrentUmbracoPage();
        }

        public ActionResult RenderVerifyEmail(string guid)
        {
            LogHelper.Info<SimpilyAuthSurfaceController>("Verifiing: {0}", () => guid);

            var memberService = ApplicationContext.Services.MemberService;

            Guid userKey;
            if (Guid.TryParse(guid, out userKey))
            {
                var member = memberService.GetByKey(userKey);

                if (member != null)
                {
                    member.SetValue(SimpilyAuth.AccountVerifiedProperty, true);
                    memberService.Save(member);
                    return Content("Account Verified");
                }
                else
                {
                    return Content(
                        GetDictionaryOrDefault("SimpleAuth.Verfiy.NoAccount", "Can't find account for guid"));
                }
            }
            else
            {
                return Content(
                    GetDictionaryOrDefault("SimpleAuth.Verify.NoAccount", "Not a valid account guid"));
            }
        }

        private bool EmailAddressExists(string emailAddress)
        {
            var email = Members.GetByEmail(emailAddress);

            if (email != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public JsonResult CheckForEmailAddress(string emailAddress)
        {
            if ( EmailAddressExists(emailAddress))
            {
                return Json(string.Format("The email {0} is already in use", emailAddress));
            }
            else
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        private bool IsValidEmailDomain(string emailAddress)
        {
            var whitelist = CurrentPage.GetPropertyValue<string>("domainWhitelist", "").ToLower();
            var blacklist = CurrentPage.GetPropertyValue<string>("domainBlacklist", "").ToLower();

            LogHelper.Info<SimpilyAuthSurfaceController>("Domain WhiteList: {0}", () => whitelist);
            LogHelper.Info<SimpilyAuthSurfaceController>("Domain Blacklist: {0}", () => blacklist);

            if (emailAddress.Contains("@"))
            {
                var domain = emailAddress.Substring(emailAddress.IndexOf("@")).ToLower();
                LogHelper.Info<SimpilyAuthSurfaceController>("Domain Check: {0}", () => domain);

                if (!string.IsNullOrWhiteSpace(whitelist) && !whitelist.Contains(domain))
                {
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(blacklist) && blacklist.Contains(domain))
                {
                    return false;
                }
            }

            return true;
        }

        public JsonResult CheckForValidEmail(string emailAddress)
        {
            if (IsValidEmailDomain(emailAddress))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("you cannot register for this site with that email address");
            }
        }

        private string GetDictionaryOrDefault(string key, string defaultValue)
        {
            LogHelper.Info<SimpilyAuthSurfaceController>("Getting Dictionary Value: {0} {1}", () => key, () => defaultValue);

            var dictionaryValue = Umbraco.GetDictionaryValue(key);
            if ( string.IsNullOrEmpty(dictionaryValue))
                return defaultValue;

            return dictionaryValue;
        }
    }
}