using BiblePlaylist.Shared.Bible;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BiblePlaylist.Shared.ExtensionMethods;

namespace BiblePlaylist.Shared.Playlist
{
    
    public class BookChapter
    {
        private string _display;
        private string _voiceText;

        public string VersionCode { get; set; }
                
        public Book Book { get; set; }
        public int BookNumber { get; set; }        
        public Chapter Chapter { get; set; }
        public int ChapterNumber { get; set; }
        public IList<Segment> Segments { get; set; }
        [JsonIgnore]
        public string Display
        {
            get => BuildDisplayReference();
            set => _display = value;
        }

        [JsonIgnore]
        public string VoiceText
        {
            get => BuildChapterVoiceText();
            set => _voiceText = value;
        }

        private string BuildDisplayReference()
        {
            if (_display != null)
                return _display;

            string bookName = Book?.Name ?? $"Book {BookNumber}";
            string chapterStr = ChapterNumber.ToString();

            if (Segments == null || !Segments.Any())
            {
                return $"{bookName} {chapterStr}";
            }

            var ranges = Segments
                .OrderBy(s => s.VerseStart)
                .Select(s =>
                    s.VerseStart == s.VerseEnd
                        ? s.VerseStart.ToString()
                        : $"{s.VerseStart}-{s.VerseEnd}")
                .ToList();

            string versePart = string.Join("; ", ranges);

            return $"{bookName} {chapterStr}:{versePart}";
        }

        private string BuildChapterVoiceText()
        {
            if (_voiceText != null)
                return _voiceText;

            string bookName = Book?.Name ?? $"Book {BookNumber}";
            return $"{bookName} chapter {ChapterNumber.NumberToText()}";
        }
        public BookChapter() { }
    }
}
