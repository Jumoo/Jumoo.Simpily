using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace SimpleAuth
{
    public class SimpleLoginViewModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Enter your password")]
        public string Password { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string ReturnUrl { get; set; }
    }

    public class SimpleForgotPasswordModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }
    }

    public class SimplePasswordResetModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }

        [DisplayName("New Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "Please enter your password")]
        // [EqualTo("Password", ErrorMessage = "You're passwords must match")]
        public string ConfirmPassword { get; set; }
    }

    public class SimpleRegisterViewModel
    {
        [DisplayName("Name")]
        [Required(ErrorMessage = "Enter your name")]
        public string Name { get; set; }

        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid Email address")]
        [Remote("CheckForEmailAddress", "SimpleAuthSurface", ErrorMessage = "The email address is already in use")]
        public string EmailAddress { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "Please enter your password")]
        // [EqualTo("Password", ErrorMessage = "You're passwords must match")]
        public string ConfirmPassword { get; set; }
    }
}