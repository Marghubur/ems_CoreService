using ModalLayer.Modal;
using System.Collections.Concurrent;
using System.Data;

namespace ServiceLayer.Caching
{
    public enum CacheTable
    {
        MasterConnections = 1,
        AccessLevel = 2,
        LeavePlan = 3,
        Company = 4
    }

    public class Cache
    {
        private readonly ConcurrentDictionary<CacheTable, dynamic> _table;
        private static readonly object _lock = new object();
        private static Cache _cache = null;

        private Cache()
        {
            _table = new ConcurrentDictionary<CacheTable, dynamic>();
        }

        public bool IsEmpty()
        {
            return _table.Count == 0 ? true : false;
        }

        public static Cache GetInstance()
        {
            if (_cache == null)
            {
                lock (_lock)
                {
                    if (_cache == null)
                    {
                        _cache = new Cache();
                    }
                }
            }
            return _cache;
        }

        public dynamic Get(CacheTable key)
        {
            dynamic value = null;
            if (_table.ContainsKey(key))
            {
                _table.TryGetValue(key, out value);
                return value;
            }

            return value;
        }

        public void AddOrUpdate(CacheTable key, dynamic value)
        {
            dynamic oldValue = null;
            if (_table.ContainsKey(key))
            {
                _table.TryGetValue(key, out oldValue);
                _table.TryUpdate(key, value, oldValue);
            }
            else
            {
                _table.TryAdd(key, value);
            }
        }

        public void Clean()
        {
            _table.Clear();
        }

        public void Remove(CacheTable cacheTable)
        {
            _table.TryRemove(cacheTable, out _);
        }

        public void ReLoad(CacheTable tableName, DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                dynamic oldTable = default(dynamic);
                switch (tableName)
                {
                    case CacheTable.AccessLevel:
                        _table.TryRemove(CacheTable.AccessLevel, out oldTable);
                        _table.TryAdd(CacheTable.AccessLevel, table);
                        break;
                    case CacheTable.LeavePlan:
                        _table.TryRemove(CacheTable.LeavePlan, out oldTable);
                        _table.TryAdd(CacheTable.LeavePlan, table);
                        break;
                    case CacheTable.Company:
                        _table.TryRemove(CacheTable.Company, out oldTable);
                        _table.TryAdd(CacheTable.Company, table);
                        break;
                }
            }
        }
    }
}