using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Audio
{
    public class WordSegment
    {
        public int SegentID { get; set; }
        public string Word { get; set; }        
        public decimal Start { get; set; }
        public decimal End { get; set; }
    }
}
