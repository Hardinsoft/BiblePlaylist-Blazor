using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Navigation
{
    public class NavChapter
    {
        public int Number { get; set; }        
        public IList<int> Verses { get; set; }
        public string AudioUrl { get; set; }
    }
}
