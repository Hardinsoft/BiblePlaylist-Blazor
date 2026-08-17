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


namespace BiblePlaylist.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LibraryController : ControllerBase
    {
        
        private readonly ILogger<LibraryController> logger;
        private readonly ILibraryRepository libraryRepository;

        public LibraryController(ILogger<LibraryController> logger, ILibraryRepository playlistRepository)
        {
            this.logger = logger;
            this.libraryRepository = playlistRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string key)
        {
            this.logger.LogDebug("Get Library", null);

            var userData =  await this.libraryRepository.GetAsync(key);
            var serializedUserData = JsonConvert.SerializeObject(userData);
            var deserializedUserData = JsonConvert.DeserializeObject<Library>(serializedUserData);

            if (userData == null)
                return NotFound();
            
            return Ok(deserializedUserData);
        }

        /// <summary>
        /// Persist the full library (including playlist segment order after drag-reorder).
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Library library)
        {
            if (library == null || string.IsNullOrWhiteSpace(library.Key))
                return BadRequest("Library with a valid Key is required");

            await this.libraryRepository.UpdateAsync(library);
            return Ok();
        }

        
        [HttpPost]
        [Route("/library/addprototype")]
        public async Task AddPrototype()
        {            
            await this.libraryRepository.AddAsync();
        }
    }
}
