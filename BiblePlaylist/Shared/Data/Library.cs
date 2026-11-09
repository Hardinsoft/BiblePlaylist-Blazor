using BiblePlaylist.Shared.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Data
{
    public class Library
    {
        public string Key {  get; set; }
        public string UPN { get; set; }
        public string Code { get; set; }
        public List<Playlist.Playlist> Playlists { get; set; } = new List<Playlist.Playlist>();
        public Library() { }

        /// <summary>
        /// Retrofits stable segment Ids across the whole library (#25): any
        /// segment saved before Id existed gets one, deterministically, so
        /// every loader agrees on the Ids until a save persists them.
        /// </summary>
        public void EnsureSegmentIds()
        {
            if (Playlists == null) return;
            foreach (var playlist in Playlists)
            {
                if (playlist?.BookChapters == null) continue;
                foreach (var chapter in playlist.BookChapters)
                {
                    if (chapter?.Segments == null) continue;
                    Segment.EnsureIds(chapter.Segments);
                }
            }
        }
    }
}
