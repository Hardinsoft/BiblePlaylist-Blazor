using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Utilities
{
    public class Cache : ICache
    {
        private IMemoryCache _cacheInstance;

        public Cache(IMemoryCache cacheInstance)
        {
            _cacheInstance = cacheInstance;
        }

        public const int OneSecond = 1;
        public const int OneMinute = 60;
        public const int OneHour = 60 * 60;
        public const int EightHours = OneHour * 8;
        public const int TwentyFourHours = OneHour * 24;
        public const int OneYear = TwentyFourHours * 365;

 
        public static string GetVersionCacheKey(string versionCode)
        {
            var key = $"Version_{versionCode}";            
            return key;
        }

        /// <summary>
        /// Gets an object from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached object (if available), otherwise null.</returns>
        public T Get<T>(string key)
        {
            return _cacheInstance.Get<T>(key);
        }

        /// <summary>
        /// Sets an object into the cache.
        /// </summary>
        /// <param name="key">The cache key to associate with the object.</param>
        /// <param name="data">The object to set in the cache.</param>
        /// <param name="cacheTime">The duration (in minutes) to cache the object.</param>
        public void Set(string key, object data, int cacheTime)
        {
            if (data != null)
            {
                _cacheInstance.Set(key, data,
                new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheTime)));
            }
        }

        /// <summary>
        /// Indicates if the specified cache key has been set.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns>Returns true if the cache key has been set, otherwise false.</returns>
        public bool IsSet(string key)
        {
            return (_cacheInstance.Get(key) != null);
        }

        /// <summary>
        /// Removes the specified key from the cache.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        public void Invalidate(string key)
        {
            _cacheInstance.Remove(key);
        }

        /// <summary>
        /// Removes the specified key from the cache.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        public void Invalidate(object key)
        {
            _cacheInstance.Remove(key);
        }

        /// <summary>
        /// Removes all cache.
        /// </summary>        
        public void InvalidateAll()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var _entries = _cacheInstance.GetType().GetField("_entries", flags).GetValue(_cacheInstance) as IDictionary<string, object>;
            List<object> cacheKeysToInvalidate = _entries.Values.OfType<ICacheEntry>()
            .Select(ce => ce.Key)
            .ToList();

            cacheKeysToInvalidate.ForEach(k => _cacheInstance.Remove(k));
        }

        /// <summary>
        /// Removes all cache.
        /// </summary>       
        public void InvalidateContains(string value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            IDictionary<string, object> _entries = _cacheInstance.GetType().GetField("_entries", flags).GetValue(_cacheInstance) as IDictionary<string, object>;
            List<object> cacheKeysToInvalidate = _entries.Values.OfType<ICacheEntry>()
            .Where(ce => ce.Key.ToString().Contains(value))
            .Select(ce => ce.Key)
            .ToList();

            cacheKeysToInvalidate.ForEach(k => _cacheInstance.Remove(k));
        }
               
        
    }
}
