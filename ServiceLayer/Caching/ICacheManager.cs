using ModalLayer.Modal;
using System;
using System.Data;

namespace ServiceLayer.Caching
{
    public interface ICacheManager
    {
        bool IsEmpty();
        dynamic Get(CacheTable key);
        void Add(CacheTable key, DataTable value);
        void Clean();
        void ReLoad(CacheTable tableName, DataTable table);
    }
}