using BiblePlaylist.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BiblePlaylist.Server.Data;
using System.Net.Http;

namespace BiblePlaylist.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BibleMenuController : ControllerBase
    {
        
        private readonly ILogger<BibleMenuController> logger;
        private readonly IBibleMenuRepository bibleMenuRepository;


        public BibleMenuController(ILogger<BibleMenuController> logger, IBibleMenuRepository bibleMenuRepository)
        {
            this.logger = logger;
            this.bibleMenuRepository = bibleMenuRepository;
        }
        [HttpGet]
        public async Task<IActionResult> Get(string key)
        {
            this.logger.LogDebug("Get Menu", null);

            var menu =  await this.bibleMenuRepository.GetAsync(key);

            if (menu == null)
                return NotFound();          
                
            
            return Ok(menu);
        }

        [HttpPost]
        public async Task Post(Shared.Bible.Version version)
        {            
            this.logger.LogDebug("Add Menu", null);
            await this.bibleMenuRepository.AddAsync(version);            
        }        
    }
}
