using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiblePlaylist.Shared.Utilities;

namespace BiblePlaylist.Shared.Playlist
{
    public class Playlist
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Shuffle { get; set; } = false;
        public bool Repeat { get; set; } = false;
        public IList<BookChapter> BookChapters { get; set; }

        public Playlist()
        {
            if(Key == null)
                Key = StringUtilities.GetShortGuid();
        }
    }
}
