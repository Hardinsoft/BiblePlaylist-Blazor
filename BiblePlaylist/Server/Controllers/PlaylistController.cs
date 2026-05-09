using BiblePlaylist.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BiblePlaylist.Server.Data;
using System.Net.Http;
using Newtonsoft.Json;
using BiblePlaylist.Shared.Data;
using BiblePlaylist.Shared.Playlist;
using Microsoft.AspNetCore.DataProtection.KeyManagement;


namespace BiblePlaylist.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaylistController : ControllerBase
    {
        
        private readonly ILogger<PlaylistController> logger;
        private readonly IVersionRepository versionRepository;

        public PlaylistController(ILogger<PlaylistController> logger, IVersionRepository versionRepository)
        {
            this.logger = logger;
            this.versionRepository = versionRepository;
        }

        [HttpPost]
        [Route("/playlist/inflate")]
        public async Task<IActionResult> Inflate(string key, List<BookChapter> bookChapters)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Version key is required");

            if (bookChapters == null || !bookChapters.Any())
                return Ok(new List<BookChapter>()); // or BadRequest if you prefer

            var version = await this.versionRepository.GetAsync(key);
            if (version == null)
                return NotFound("Bible version not found");

     

            // Create fast lookup: (BookNumber, ChapterNumber) → full Chapter object from version
            var chapterLookup = version.Books
                .SelectMany(b => b.Chapters, (b, c) => new { Book = b, Chapter = c })
                .ToDictionary(
                    x => (x.Book.Number, x.Chapter.Number),
                    x => x);

            // Now preserve the exact order the client sent
            var result = new List<BookChapter>();

            foreach (var provided in bookChapters)
            {
                var keyTuple = (provided.BookNumber, provided.ChapterNumber);

                if (!chapterLookup.TryGetValue(keyTuple, out var match))
                {
                    // Chapter not found in this version → skip or handle error
                    // For now we just skip – you could also collect errors
                    continue;
                }

                var segments = provided.Segments.Select(seg =>
                {
                    var filteredVerses = match.Chapter.Verses
                        .Where(v => v.Number >= seg.VerseStart && v.Number <= seg.VerseEnd)
                        .ToList();

                    return new Segment
                    {
                        Verses = filteredVerses,
                        VerseStart = seg.VerseStart,
                        VerseEnd = seg.VerseEnd,
                        // VoiceText = ... if you compute it here or leave as-is
                    };
                }).ToList();

                var enriched = new BookChapter
                {
                    VersionCode = version.Code,
                    Book = match.Book,
                    BookNumber = match.Book.Number,
                    Chapter = match.Chapter,
                    ChapterNumber = match.Chapter.Number,
                    Segments = segments,
                    // Display and VoiceText will be computed by getters
                };

                result.Add(enriched);
            }

            return Ok(result);
        }



    }
}
