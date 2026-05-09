using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BiblePlaylist.Shared.Navigation
{
    public class NavBook
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string AltNames { get; set; }
        public Bible.Testament Testament { get; set; }
        public IList<NavChapter> Chapters { get; set; }
        
        
        
    }
}
