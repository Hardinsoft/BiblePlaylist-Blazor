using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Utilities
{
    public static class StringUtilities
    {
        /// <summary>
        /// A partial implementation of Mads' short guid
        /// https://www.madskristensen.net/blog/A-shorter-and-URL-friendly-GUID
        /// </summary>
        /// <returns>A shorter guid</returns>
        public static string GetShortGuid()
        {
            string enc = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            enc = enc.Replace("/", "_");
            enc = enc.Replace("+", "-");
            return enc.Substring(0, 22);
        }
    }
}
