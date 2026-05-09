using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.ExtensionMethods
{
    /// <summary>
    /// This static extension method converts string to Windows filename
    /// Source: https://stackoverflow.com/questions/620605/how-to-make-a-valid-windows-filename-from-an-arbitrary-string
    /// <returns>
    /// string
    /// </returns>
    /// <example>
    /// For example:
    /// <code>
    /// string s = "greg.hardin@live.com";
    /// string filename = x.ToFilename();
    /// </code>
    /// Results in <c>filename</c>'s having the value greg_hardin_live_com
    /// </example>
    /// <param>int n</param>
    /// </summary>

    public static class StringExtensionMethods
    {
        public static string ToFilename(this string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(s);
            var invalidChars = Path.GetInvalidFileNameChars().ToList();            
            invalidChars.AddRange(new List<Char> { '.', '@' });

            foreach (var c in invalidChars)
                sb.Replace(c, '_');
            
            return sb.ToString();
        }
    }
}
