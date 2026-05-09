using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BiblePlaylist.Shared.Bible
{
    public class Chapter
    {
        public int Number { get; set; }
        public string AudioUrl { get; set; }
        public IList<Verse> Verses { get; set; }
        [JsonIgnore]
        public string PaddedNumber { get { return ((Number < 10) ? Number.ToString().PadLeft(2, '0') : Number.ToString()); } }
    }
}
