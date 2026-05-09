using System;
using System.Collections.Generic;
using System.Text;

namespace BiblePlaylist.Shared.DTO
{
    public class VerseDTO
    {
        public string BookName { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Text { get; set; }
    }
}
