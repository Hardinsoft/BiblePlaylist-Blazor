using BiblePlaylist.Shared.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Server.Data
{
    public interface ILibraryRepository
    {
        Task AddAsync();
        Task<Library> GetAsync(string key);        
        Task UpdateAsync(Library version);
    }
}
