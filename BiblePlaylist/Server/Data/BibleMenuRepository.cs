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
using BiblePlaylist.Shared.Navigation;

namespace BiblePlaylist.Server.Data
{
    public class BibleMenuRepository : BaseRepository, IBibleMenuRepository
    {        
        private readonly ILogger<BibleMenuRepository> _logger;
        private readonly IVersionRepository _versionRepository;

        public BibleMenuRepository(IVersionRepository versionRepository, ILogger<BibleMenuRepository> logger, ICache cache) : base(cache)
        {            
            _logger = logger;
            _versionRepository = versionRepository;

        }
        public async Task AddAsync(Shared.Bible.Version version)
        {
            var inflatedVersion = await _versionRepository.GetAsync(version.Code);

            var menu = new BibleMenu() { Books = new List<NavBook>() }; 

            foreach (var book in inflatedVersion.Books)
            {
                if (book.Chapters == null) break;
                var navbook = new NavBook() { Number = book.Number, AltNames = book.AltNames, Name = book.Name, Testament = book.Testament, Chapters = new List<NavChapter>() };
                
                var bookName = book.Name;
                foreach (var chapter in book.Chapters)
                {
                    var navChapter = new NavChapter() { Number = chapter.Number, Verses = new List<int>(), AudioUrl = chapter.AudioUrl };
                   
                    var chapterNumber = chapter.Number;
                    foreach (var verse in chapter.Verses)
                    {
                        var verseNumber = verse.Number;                        
                        navChapter.Verses.Add(verseNumber);
                        _logger.LogInformation($"Adding Menu Item {bookName} {chapterNumber}:{verseNumber}");                        
                    }
                    navbook.Chapters.Add(navChapter);
                }
                menu.Books.Add(navbook);
            }

            await SaveEntityAsync(menu);
        }

        public async Task<Shared.Navigation.BibleMenu> GetAsync(string key)
        {
            var bibleMenu = await GetEntityAsync<Shared.Navigation.BibleMenu>(key);
            return bibleMenu;
        }      


    }
}


