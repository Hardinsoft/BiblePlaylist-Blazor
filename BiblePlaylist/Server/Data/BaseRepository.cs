using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiblePlaylist.Shared;
using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.Utilities;
using Newtonsoft.Json;

namespace BiblePlaylist.Server.Data
{
    public abstract class BaseRepository
    {
        private readonly ICache _cache;

        public BaseRepository(ICache cache)
        {
            _cache = cache;
        }

        protected async Task SaveEntityAsync(object data)
        {
            var codeValue = data.GetType().GetProperty("Code").GetValue(data);
            string json = JsonConvert.SerializeObject(data);
            await File.WriteAllTextAsync($"{Directory.GetCurrentDirectory()}/Storage/{codeValue?.ToString()}.json", json);
            _cache.Set(Cache.GetVersionCacheKey(codeValue?.ToString()), data, Cache.OneYear);
        }

        

        protected async Task<T> GetEntityAsync<T>(string key)
        {
            var cachedObject = _cache.Get<T>(Cache.GetVersionCacheKey(key));
            if (cachedObject == null)
            {
                var json = await File.ReadAllTextAsync($"{Directory.GetCurrentDirectory()}/Storage/{key}.json");
                cachedObject = JsonConvert.DeserializeObject<T>(json);
                _cache.Set(Cache.GetVersionCacheKey(key), cachedObject, Cache.OneYear);
            }
            //else
           // {
           //     var serializedCachedObject = JsonConvert.SerializeObject(cachedObject);
           //     cachedObject = JsonConvert.DeserializeObject<T>(serializedCachedObject);
           // }
            return cachedObject;
        }


    }
}

// sample audio link https://feeds.bible.org/netaudio/09-1Samuel-01.mp3
