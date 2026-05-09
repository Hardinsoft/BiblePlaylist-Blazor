using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiblePlaylist.Shared;
using BiblePlaylist.Shared.Bible;
using Newtonsoft.Json;
using System.Net.Http;
using BiblePlaylist.Shared.DTO;
using System.Threading;
using System.Net;
using Microsoft.Extensions.Logging;
using BiblePlaylist.Shared.Utilities;
using BiblePlaylist.Shared.Playlist;
using BiblePlaylist.Shared.Data;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Identity;
using BiblePlaylist.Shared.ExtensionMethods;

namespace BiblePlaylist.Server.Data
{
    public class LibraryRepository : BaseRepository, ILibraryRepository
    {
        private readonly HttpClient _httpText;
        private readonly HttpClient _httpAudio;
        private readonly ILogger<LibraryRepository> _logger;        

        public LibraryRepository(IHttpClientFactory httpClientFactory, ILogger<LibraryRepository> logger, ICache cache) : base(cache)
        {
            _httpText = httpClientFactory.CreateClient("text");
            _httpAudio = httpClientFactory.CreateClient("audio");
            _logger = logger;

        }
        public async Task AddAsync()
        {
            var library = new Library()
            {
                Key = Guid.NewGuid().ToString(),
                UPN = "greg.hardin@live.com",
                Code = "library_greg.hardin@live.com".ToFilename(),
                Playlists = new List<Playlist>()
                {
                    new Playlist()
                    {
                        Name = "The Christmas Story",
                        Description = "The Christmas Story from John, Luke, and Matthew",
                        Repeat = true,
                        BookChapters = new List<BookChapter>()
                        {
                            new BookChapter()
                            {
                                BookNumber = 43,
                                ChapterNumber = 1,
                                Display = "John 1:1-9",
                                VersionCode = "NET",
                                VoiceText = "John chapter 1",
                                Segments = new List<Segment>()
                                {
                                    new Segment()
                                    {
                                         VerseStart = 1,
                                         VerseEnd = 9,
                                         VoiceText = "verses 1 through 9"
                                    }
                                }

                            },
                            new BookChapter()
                            {
                                BookNumber = 42,
                                ChapterNumber = 1,
                                Display = "Luke 1:26-47",
                                VersionCode = "NET",
                                VoiceText = "Luke chapter 1",
                                Segments = new List<Segment>()
                                {
                                    new Segment()
                                    {
                                         VerseStart = 26,
                                         VerseEnd = 47,
                                         VoiceText = "verses 26 through 47"
                                    }
                                }

                            },
                            new BookChapter()
                            {
                                BookNumber = 40,
                                ChapterNumber = 1,
                                Display = "Matthew 1:17-25",
                                VersionCode = "NET",
                                VoiceText = "Matthew chapter 1",
                                Segments = new List<Segment>()
                                {
                                    new Segment()
                                    {
                                         VerseStart = 17,
                                         VerseEnd = 25,
                                         VoiceText = "verses 17 through 25"
                                    }
                                }

                            },
                            new BookChapter()
                            {
                                BookNumber = 42,
                                ChapterNumber = 2,
                                Display = "Luke 2:1-39",
                                VersionCode = "NET",
                                VoiceText = "Luke chapter 2",
                                Segments = new List<Segment>()
                                {
                                    new Segment()
                                    {
                                         VerseStart = 1,
                                         VerseEnd = 39,
                                         VoiceText = "verses 1 through 39"
                                    }
                                }

                            },
                            new BookChapter()
                            {
                                BookNumber = 40,
                                ChapterNumber = 2,
                                Display = "Matthew 2:1-15",
                                VersionCode = "NET",
                                VoiceText = "Matthew chapter 2",
                                Segments = new List<Segment>()
                                {
                                    new Segment()
                                    {
                                         VerseStart = 1,
                                         VerseEnd = 15,
                                         VoiceText = "verses 1 through 15"
                                    }
                                }

                            }

                        }
                    }

                }

            };

            await SaveEntityAsync(library);
        }

        public async Task<Library> GetAsync(string key)
        {
            var library = await GetEntityAsync<Library>(key);
            return library;
        }

        public async Task UpdateAsync(Library library)
        {            
            await SaveEntityAsync(library);
        }       


    }
}

// sample audio link https://feeds.bible.org/netaudio/09-1Samuel-01.mp3
