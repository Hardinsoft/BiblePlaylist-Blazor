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

        [JsonIgnore]
        public string VoiceText 
        {
            get => BuildVoiceText();
            set => _voiceText = value;
        }
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
