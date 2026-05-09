using BiblePlaylist.Server.Data;
using BiblePlaylist.Shared;
using BiblePlaylist.Shared.Bible;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace BiblePlaylist.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {

        private readonly ILogger<VersionController> logger;
        private readonly IVersionRepository versionRepository;

        public VersionController(ILogger<VersionController> logger, IVersionRepository versionRepository)
        {
            this.logger = logger;
            this.versionRepository = versionRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string key, int? book = null, int? chapter = null)
        {
            this.logger.LogDebug("Get Version", null);

            var version = await this.versionRepository.GetAsync(key);
            var serializedVersion = JsonConvert.SerializeObject(version);
            var deserializedVersion = JsonConvert.DeserializeObject<Shared.Bible.Version>(serializedVersion);

            if (version == null)
                return NotFound();

            if (book.HasValue)
            {
                deserializedVersion.Books = deserializedVersion.Books.Where(b => b.Number == book.Value).ToList();
                deserializedVersion.Books[0].Chapters = deserializedVersion.Books[0].Chapters.Where(c => c.Number == chapter.Value).ToList();
            }
            return Ok(deserializedVersion);
        }

        [HttpPost]
        public async Task Post()
        {
            this.logger.LogDebug("Add Version", null);
            await this.versionRepository.AddAsync();
        }

        [HttpPut]
        public async Task Put(Shared.Bible.Version version)
        {
            this.logger.LogDebug("Update Version", null);
            await this.versionRepository.UpdateAsync(version.Code);
        }

        [HttpPut]
        [Route("/version/save")]
        public async Task Save(Shared.Bible.Version version)
        {
            this.logger.LogDebug("Update Version", null);
            await this.versionRepository.UpdateAsync(version);
        }
        [HttpPost]
        [Route("/version/saveverses")]
        public async Task SaveVerses(Shared.Bible.Version partialVersion)
        {
            var version = await this.versionRepository.GetAsync(partialVersion.Code);
            var book = partialVersion.Books.FirstOrDefault();
            var chapter = book.Chapters.FirstOrDefault();
            var changedVerses = chapter.Verses;

            var verses = version.Books.FirstOrDefault(b => b.Number == book.Number).Chapters.FirstOrDefault(c => c.Number == chapter.Number).Verses;

            foreach (var verse in verses)
            {
                var cv = changedVerses.FirstOrDefault(v => v.Number == verse.Number);
                verse.AudioStart = cv.AudioStart;
                verse.AudioEnd = cv.AudioEnd;
            }
            await this.versionRepository.UpdateAsync(version);
        }

        [HttpPost]
        [Route("/version/audio")]
        public async Task<IActionResult> SaveAudio(string filename)
        {
            try
            {               

               var result = await this.versionRepository.SaveAudioAsync(filename);

                if (result == filename)
                    return Ok($"{filename} uploaded successfully.");
                else
                    return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving file: {ex.Message}");
            }

        }

        [HttpPost]
        [Route("/version/parseverseaudio")]
        public async Task<IActionResult> ParseVerseAudio(Shared.Bible.Version partialVersion)
        {
            var book = partialVersion?.Books?.FirstOrDefault();
            var chapter = book?.Chapters?.FirstOrDefault();
            try
            {
                var result = await this.versionRepository.ParseVerseAudio(partialVersion);
                await SaveVerses(result);

                if (result != null)
                    return Ok();
                else
                {
                    this.logger.LogWarning($"VersionController.ParseVerseAudio: No data returned for {book?.Number} {chapter?.Number}");
                    return BadRequest("No data returned");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"VersionController.ParseVerseAudio: Error parsing verse audio for {book?.Number} {chapter?.Number}");
                return StatusCode(500, $"Error saving file: {ex.Message}");
            }

        }
    }
}
