using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Server.Data
{
    public interface IBibleMenuRepository
    {
        public Task AddAsync(Shared.Bible.Version version);
        Task<Shared.Navigation.BibleMenu> GetAsync(string key);       
    }
}
