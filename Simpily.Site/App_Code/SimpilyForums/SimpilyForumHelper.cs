using System;
using System.Security.Cryptography;
using System.Text;

namespace Jumoo.Simpily
{
    /// <summary>
    // Couple of helper functions, to make the views eaiser to read
    /// </summary>
    public class ForumHelper
    {
        public static string GravatarURL(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return "http://www.gravatar.com/avatar/00000000000000000000000000000000?d=mm&f=y"; // the gray man...

            //Get email to lower
            var emailToHash = emailAddress.ToLower();

            // Create a new instance of the MD5CryptoServiceProvider object.  
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(emailToHash));

            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string.  
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            var hashedEmail = sBuilder.ToString();  // Return the hexadecimal string.

            //Return the gravatar URL
            return "http://www.gravatar.com/avatar/" + hashedEmail + "?d=mm";
        }

        public static string GetRelativeDate(DateTime date)
        {
            // takes a date and displays a relative thing
            // i.e 10minutes ago, 1 day ago...

            if (date == DateTime.MinValue)
                return "never";

            var span = DateTime.Now.Subtract(date);

            if (span.Days > 0)
            {
                if (span.Days > 7)
                {
                    return date.ToString("dd MMM yyyy H:ss");
                }
                else if (span.Days == 1)
                {
                    return "yesterday";
                }
                else
                {
                    return string.Format("{0} days ago", span.Days);
                }
            }
            else if (span.Hours > 0)
            {
                return string.Format("{0} hour{1} ago", span.Hours, span.Hours > 1 ? "s" : "");
            }
            else if (span.Minutes > 0)
            {
                return string.Format("{0} minute{1} ago", span.Minutes, span.Minutes > 1 ? "s" : "");
            }
            else
            {
                return "just now";
            }
        }
    }
}