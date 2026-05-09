using System;
using System.Collections.Generic;
using System.Text;

namespace BiblePlaylist.Shared.Bible
{
    public class Verse
    {
        public int Number { get; set; }
        public string Html { get; set; }        
        public decimal AudioStart { get; set; }
        public decimal AudioEnd { get; set; }
    }
}
