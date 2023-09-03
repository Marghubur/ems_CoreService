using ModalLayer.Modal;
using System.Data;

namespace ServiceLayer.Caching
{
    public class CacheManager : ICacheManager
    {
        private static readonly object _lock = new object();
        private static CacheManager _cacheManager;

        private Cache _cache = null;

        private CacheManager() { }

        public static CacheManager GetInstance(string connectionString)
        {
            if (_cacheManager == null)
            {
                lock (_lock)
                {
                    if (_cacheManager == null)
                    {
                        _cacheManager = new CacheManager();
                        _cacheManager._cache = Cache.GetInstance();
                    }
                }
            }
            return _cacheManager;
        }

        public bool IsEmpty()
        {
            return _cache.IsEmpty();
        }

        public void Add(CacheTable key, DataTable value)
        {
            _cache.AddOrUpdate(key, value);
        }

        public void Clean()
        {
            _cache.Clean();
        }

        public dynamic Get(CacheTable key)
        {
            if (_cache.IsEmpty())
            {
                if (_cache.IsEmpty())
                    throw new HiringBellException("Encounter some internal issue. Please login again or contact to your admin.");
            }

            return _cache.Get(key);
        }

        public void ReLoad(CacheTable tableName, DataTable table)
        {
            _cache.ReLoad(tableName, table);
        }
    }
}