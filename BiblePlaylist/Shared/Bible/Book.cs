using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BiblePlaylist.Shared.Bible
{
    public class Book
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string AltNames { get; set; }
        public Testament Testament { get; set; }
        public IList<Chapter> Chapters { get; set; }
        [JsonIgnore]
        public string PaddedNumber { get { return ((Number < 10) ? Number.ToString().PadLeft(2, '0') : Number.ToString()); }}
        
    }
}
