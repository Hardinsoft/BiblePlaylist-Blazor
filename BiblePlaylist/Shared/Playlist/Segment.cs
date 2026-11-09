using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Playlist
{
    public class Segment
    {
        private string _voiceText;

        /// <summary>
        /// Stable identity within one chapter's segment list (#25). Ids only
        /// need to be unique per chapter: every lookup is scoped to a
        /// chapter. 0 means "not yet assigned" (data saved before Id existed).
        /// Small on purpose: Greg plans to share playlists as a compressed
        /// string, and ints compress far better than Guids.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Assigns Ids to any segment that lacks one (Id == 0), deterministically:
        /// in list order, each gets max(existing Ids) + 1. Existing Ids are
        /// never changed. Deterministic so every loader computes the same Ids
        /// for the same file until a save persists them.
        /// </summary>
        public static void EnsureIds(IList<Segment> segments)
        {
            if (segments == null) return;
            var max = 0;
            foreach (var s in segments)
                if (s.Id > max) max = s.Id;
            foreach (var s in segments)
                if (s.Id == 0)
                    s.Id = ++max;
        }

        [JsonIgnore]
        public string VoiceText 
        {
            get => BuildVoiceText();
            set => _voiceText = value;
        }
        [Newtonsoft.Json.JsonIgnore]
        public IList<Verse> Verses { get; set; }
        public int VerseStart { get; set; }
        public int VerseEnd {  get; set; }
        private string BuildVoiceText()
        {
            if (_voiceText != null)
                return _voiceText;

            if (VerseStart == VerseEnd)
            {
                return $"verse {VerseStart.NumberToText()}";
            }
            else
            {
                return $"verses {VerseStart.NumberToText()} through {VerseEnd.NumberToText()}";
            }
        }
        public Segment() { }
    }
}
