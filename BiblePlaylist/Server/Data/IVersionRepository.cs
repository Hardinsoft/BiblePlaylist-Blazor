using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Server.Data
{
    public interface IVersionRepository
    {
        public Task AddAsync();
        Task<Shared.Bible.Version> GetAsync(string key);
        public Task UpdateAsync(string key);
        public Task UpdateAsync(Shared.Bible.Version version);
        Task<string> SaveAudioAsync(string fileName);
        Task<Shared.Bible.Version> ParseVerseAudio(Shared.Bible.Version partialVersion);
    }
}
