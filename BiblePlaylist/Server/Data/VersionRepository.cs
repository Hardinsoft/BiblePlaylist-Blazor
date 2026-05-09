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
using Microsoft.AspNetCore.Http;
using BiblePlaylist.Shared.Audio;

namespace BiblePlaylist.Server.Data
{
    public class VersionRepository : BaseRepository, IVersionRepository
    {
        private readonly HttpClient _httpText;
        private readonly HttpClient _httpAudio;
        private readonly ILogger<VersionRepository> _logger;
        private readonly IAudioTimingProcessor _audioTimingProcessor;

        public VersionRepository(IHttpClientFactory httpClientFactory, ILogger<VersionRepository> logger, ICache cache, IAudioTimingProcessor audioTimingProcessor) : base(cache)
        {
            _httpText = httpClientFactory.CreateClient("text");
            _httpAudio = httpClientFactory.CreateClient("audio");
            _logger = logger;
            _audioTimingProcessor = audioTimingProcessor;
        }
        public async Task AddAsync()
        {
            var version = new Shared.Bible.Version
            {
                Code = "NET",
                Name = "New English Translation",
                Books = new List<Book>
                {
                    new Book { Name = "Genesis", AltNames = "Gen", Number = 1, Testament = Testament.Old },
                    new Book { Number = 2, Name = "Exodus", AltNames = "Ex", Testament = Testament.Old },
                    new Book { Number = 3, Name = "Leviticus", AltNames = "Lev", Testament = Testament.Old },
                    new Book { Number = 4, Name = "Numbers", AltNames = "Num", Testament = Testament.Old },
                    new Book { Number = 5, Name = "Deuteronomy", AltNames = "Deut", Testament = Testament.Old },
                    new Book { Number = 6, Name = "Joshua", AltNames = "Jos", Testament = Testament.Old },
                    new Book { Number = 7, Name = "Judges", AltNames = "Jud", Testament = Testament.Old },
                    new Book { Number = 8, Name = "Ruth", AltNames = "Ruth", Testament = Testament.Old },
                    new Book { Number = 9, Name = "1 Samuel", AltNames = "1 Sam", Testament = Testament.Old },
                    new Book { Number = 10, Name = "2 Samuel", AltNames = "2 Sam", Testament = Testament.Old },
                    new Book { Number = 11, Name = "1 Kings", AltNames = "1 King", Testament = Testament.Old },
                    new Book { Number = 12, Name = "2 Kings", AltNames = "2 King", Testament = Testament.Old },
                    new Book { Number = 13, Name = "1 Chronicles", AltNames = "1 Chr", Testament = Testament.Old },
                    new Book { Number = 14, Name = "2 Chronicles", AltNames = "2 Chr", Testament = Testament.Old },
                    new Book { Number = 15, Name = "Ezra", AltNames = "Ezr", Testament = Testament.Old },
                    new Book { Number = 16, Name = "Nehemiah", AltNames = "Neh", Testament = Testament.Old },
                    new Book { Number = 17, Name = "Esther", AltNames = "Est", Testament = Testament.Old },
                    new Book { Number = 18, Name = "Job", AltNames = "Job", Testament = Testament.Old },
                    new Book { Number = 19, Name = "Psalms", AltNames = "Psa", Testament = Testament.Old },
                    new Book { Number = 20, Name = "Proverbs", AltNames = "Pro", Testament = Testament.Old },
                    new Book { Number = 21, Name = "Ecclesiastes", AltNames = "Ecc", Testament = Testament.Old },
                    new Book { Number = 22, Name = "Song of Solomon", AltNames = "Song", Testament = Testament.Old },
                    new Book { Number = 23, Name = "Isaiah", AltNames = "Isa", Testament = Testament.Old },
                    new Book { Number = 24, Name = "Jeremiah", AltNames = "Jer", Testament = Testament.Old },
                    new Book { Number = 25, Name = "Lamentations", AltNames = "Lam", Testament = Testament.Old },
                    new Book { Number = 26, Name = "Ezekiel", AltNames = "Eze", Testament = Testament.Old },
                    new Book { Number = 27, Name = "Daniel", AltNames = "Dan", Testament = Testament.Old },
                    new Book { Number = 28, Name = "Hosea", AltNames = "Hos", Testament = Testament.Old },
                    new Book { Number = 29, Name = "Joel", AltNames = "Joe", Testament = Testament.Old },
                    new Book { Number = 30, Name = "Amos", AltNames = "Amo", Testament = Testament.Old },
                    new Book { Number = 31, Name = "Obadiah", AltNames = "Oba", Testament = Testament.Old },
                    new Book { Number = 32, Name = "Jonah", AltNames = "Jon", Testament = Testament.Old },
                    new Book { Number = 33, Name = "Micah", AltNames = "Mic", Testament = Testament.Old },
                    new Book { Number = 34, Name = "Nahum", AltNames = "Nah", Testament = Testament.Old },
                    new Book { Number = 35, Name = "Habakkuk", AltNames = "Hab", Testament = Testament.Old },
                    new Book { Number = 36, Name = "Zephaniah", AltNames = "Zep", Testament = Testament.Old },
                    new Book { Number = 37, Name = "Haggai", AltNames = "Hag", Testament = Testament.Old },
                    new Book { Number = 38, Name = "Zechariah", AltNames = "Zec", Testament = Testament.Old },
                    new Book { Number = 39, Name = "Malachi", AltNames = "Mal", Testament = Testament.Old },
                    new Book { Number = 40, Name = "Matthew", AltNames = "Mth", Testament = Testament.New },
                    new Book { Number = 41, Name = "Mark", AltNames = "Mar", Testament = Testament.New },
                    new Book { Number = 42, Name = "Luke", AltNames = "Lk", Testament = Testament.New },
                    new Book { Number = 43, Name = "John", AltNames = "Jn", Testament = Testament.New },
                    new Book { Number = 44, Name = "Acts", AltNames = "Act", Testament = Testament.New },
                    new Book { Number = 45, Name = "Romans", AltNames = "Rm", Testament = Testament.New },
                    new Book { Number = 46, Name = "1 Corinthians", AltNames = "1 Cor", Testament = Testament.New },
                    new Book { Number = 47, Name = "2 Corinthians", AltNames = "2 Cor", Testament = Testament.New },
                    new Book { Number = 48, Name = "Galatians", AltNames = "Gal", Testament = Testament.New },
                    new Book { Number = 49, Name = "Ephesians", AltNames = "Eph", Testament = Testament.New },
                    new Book { Number = 50, Name = "Philippians", AltNames = "Phil", Testament = Testament.New },
                    new Book { Number = 51, Name = "Colossians", AltNames = "Col", Testament = Testament.New },
                    new Book { Number = 52, Name = "1 Thessalonians", AltNames = "1 The", Testament = Testament.New },
                    new Book { Number = 53, Name = "2 Thessalonians", AltNames = "2 The", Testament = Testament.New },
                    new Book { Number = 54, Name = "1 Timothy", AltNames = "1 Tim", Testament = Testament.New },
                    new Book { Number = 55, Name = "2 Timothy", AltNames = "2 Tim", Testament = Testament.New },
                    new Book { Number = 56, Name = "Titus", AltNames = "Titus", Testament = Testament.New },
                    new Book { Number = 57, Name = "Philemon", AltNames = "Phi", Testament = Testament.New },
                    new Book { Number = 58, Name = "Hebrews", AltNames = "Heb", Testament = Testament.New },
                    new Book { Number = 59, Name = "James", AltNames = "Jm", Testament = Testament.New },
                    new Book { Number = 60, Name = "1 Peter", AltNames = "1 Pet", Testament = Testament.New },
                    new Book { Number = 61, Name = "2 Peter", AltNames = "2 Pet", Testament = Testament.New },
                    new Book { Number = 62, Name = "1 John", AltNames = "1 Jn", Testament = Testament.New },
                    new Book { Number = 63, Name = "2 John", AltNames = "2 Jn", Testament = Testament.New },
                    new Book { Number = 64, Name = "3 John", AltNames = "3 Jn", Testament = Testament.New },
                    new Book { Number = 65, Name = "Jude", AltNames = "Jud", Testament = Testament.New },
                    new Book { Number = 66, Name = "Revelation", AltNames = "Rev", Testament = Testament.New }                    
                }
            };

            await SaveEntityAsync(version);
        }

        public async Task<Shared.Bible.Version> GetAsync(string key)
        {
            var version = await GetEntityAsync<Shared.Bible.Version>(key);
            return version;
        }

        public async Task UpdateAsync(string key)
        {
            var version = await GetEntityAsync<Shared.Bible.Version>(key);
            version = await Inflate(version);            
            await SaveEntityAsync(version);
            
        }

        public async Task UpdateAsync(Shared.Bible.Version version)
        {            
            await SaveEntityAsync(version);
        }

        public async Task<string> SaveAudioAsync(string fileName)
        {
            try
            {
                var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage", "audio");
                var filePath = Path.Combine(storagePath, fileName);

                // Check if the file already exists
                if (File.Exists(filePath))
                {
                    return $"{fileName} already exists";
                }

                var bytes = await DownloadAudioAsync(fileName);

                if(bytes == null || bytes.Length == 0)
                {
                    return $"{fileName} not found or empty";
                }
                
                if (!Directory.Exists(storagePath))
                {
                    Directory.CreateDirectory(storagePath);
                }                

                // Save the file to the server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                return $"Error saving audio file: {ex.Message}";
            }

            return fileName;
        }

        public async Task<Shared.Bible.Version> ParseVerseAudio(Shared.Bible.Version partialVersion)
        {
            var book = partialVersion?.Books?.FirstOrDefault();
            var chapter = book?.Chapters?.FirstOrDefault();
            try
            {              
                var audioUrl = chapter.AudioUrl;
                var transcriptFileName =  "audio/" + audioUrl.Substring(audioUrl.LastIndexOf("/") + 1).Replace(".mp3", "-transcription");
                var transcription = await GetEntityAsync<Transcription>(transcriptFileName);
                return _audioTimingProcessor.SetVerseTimings(partialVersion, transcription);
                
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                _logger.LogError(ex, $"VersionRepository.ParseVerseAudio: Error parsing verse audio for {book?.Number} {chapter?.Number}");
                return null;
            }            
        }

        private async Task<Shared.Bible.Version> Inflate(Shared.Bible.Version version)
        {
            int[] books = new int[] { 61, 62, 63, 64, 65, 66 };

            foreach (var book in version.Books.Where(b => books.Contains(b.Number) && b.Chapters == null))
            {
                int chapterNumber = 1;
                int verseNumber = 1;
                var bookName = book.Name;
                book.Chapters = new List<Chapter>();
                var verses = new List<Verse>();

                var verseDTO = await GetVerseDTO(bookName, chapterNumber, verseNumber);
                verses.Add(new Verse { Number = verseDTO.Verse, Html = verseDTO.Text });
                _logger.Log(LogLevel.Information, verseDTO.Text);
                verseNumber++;

                while (true)
                {
                    verseDTO = await GetVerseDTO(bookName, chapterNumber, verseNumber);
                    if (verseDTO != null)
                    {
                        if (verseNumber == verseDTO.Verse)
                        {
                            verses.Add(new Verse { Number = verseDTO.Verse, Html = verseDTO.Text });
                            _logger.Log(LogLevel.Information, verseDTO.Text);
                            verseNumber++;
                        }
                        else
                        {
                            // Found all version, save chapter and iterate to the next
                            var chapter = new Chapter() { Number = chapterNumber, Verses = JsonConvert.DeserializeObject<List<Verse>>(JsonConvert.SerializeObject(verses)) };

                            string url = $"/netaudio/{book.PaddedNumber}-{book.Name.Replace(" ", "")}-{chapter.PaddedNumber}.mp3";
                            var cts = new CancellationTokenSource();
                            cts.CancelAfter(300);

                            // Run code to check for audio url and set chapter
                            try
                            {
                                await _httpAudio.GetAsync(url, cts.Token);
                            }
                            catch (WebException ex)
                            {
                                _logger.LogError(ex, "No audio url");                                 
                            }
                            catch (TaskCanceledException tke)
                            {
                                if (tke.CancellationToken.IsCancellationRequested)
                                {
                                    // Found the file but cancelled before it could download
                                    chapter.AudioUrl = _httpAudio.BaseAddress.OriginalString + url;
                                }
                            }
                            _logger.Log(LogLevel.Information, chapter.AudioUrl);
                            book.Chapters.Add(chapter);
                            // Move to next chapter
                            chapterNumber++;
                            // Reset verse for new chapter
                            verseNumber = 1;
                            verses.Clear();
                            // Does new chapter exist
                            verseDTO = await GetVerseDTO(bookName, chapterNumber, verseNumber);

                            // if chapter can't be found we've come to the end of the book, exit while and foreach to next book
                            if (chapterNumber != verseDTO.Chapter)
                                break;

                        }
                    }
                    else
                    {
                        // Data missing pass the verse number in and leave verse blank.  Will correct it with data check
                        verses.Add(new Verse { Number = verseNumber, Html = "" });
                        _logger.Log(LogLevel.Information, "Verse left empty");
                        verseNumber++;
                    }
                }
            }
            return version;
        }

        private async Task<Shared.Bible.Version> Compress(Shared.Bible.Version version)
        {
            foreach(var book in version.Books)
            {
                var bookName = book.Name;
                foreach(var chapter in book.Chapters)
                {
                    var chapterNumber = chapter.Number;
                    foreach(var verse in chapter.Verses)
                    {
                        var verseNumber = verse.Number;
                        _logger.LogInformation($"Compressing {bookName} {chapterNumber}:{verseNumber}");
                        verse.Html = StringCompressor.CompressString(verse.Html);
                    }
                }
            }
            return  await Task.FromResult(version);
        }

        private async Task<Shared.Bible.Version> DeCompress(Shared.Bible.Version version)
        {
            foreach (var book in version.Books)
            {
                if (book.Chapters == null) break;
                var bookName = book.Name;
                foreach (Chapter chapter in book.Chapters)
                {
                    var chapterNumber = chapter.Number;
                    foreach (var verse in chapter.Verses)
                    {
                        var verseNumber = verse.Number;
                        _logger.LogInformation($"DeCompressing {bookName} {chapterNumber}:{verseNumber}");
                        verse.Html = StringCompressor.DecompressString(verse.Html);
                    }
                }
            }
            return await Task.FromResult(version);
        }

        private async Task<VerseDTO> GetVerseDTO(string bookName, int chapter, int verse)
        {
            VerseDTO verseDTO = null;          
            var verseJSON = await _httpText.GetStringAsync($"/api/?passage={bookName} {chapter}:{verse}&type=json");
            verseDTO = JsonConvert.DeserializeObject<List<VerseDTO>>(verseJSON).FirstOrDefault();
            
            return verseDTO;
        }

        private async Task<byte[]> DownloadAudioAsync(string fileName)
        {
            try
            {
                return await _httpAudio.GetByteArrayAsync($"netaudio/{fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading audio: {ex.Message}");
                return null;
            }
        }


    }
}

// sample audio link https://feeds.bible.org/netaudio/09-1Samuel-01.mp3
