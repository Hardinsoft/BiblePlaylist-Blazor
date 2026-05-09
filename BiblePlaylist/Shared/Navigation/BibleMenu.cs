using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Navigation
{
    public class BibleMenu
    {
        public string Code { get; set; } = "biblemenu";
        public IList<NavBook> Books { get; set; }
    }
}
