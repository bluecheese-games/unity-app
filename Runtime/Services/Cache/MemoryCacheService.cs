//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BlueCheese.App.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

        public IReadOnlyDictionary<string, CacheEntry> GetEntries() => _entries;

        public bool Exists(string key) => TryGet(key, out _);

        public CacheEntry Get(string key)
        {
            if (TryGet(key, out var entry))
            {
                return entry;
            }
            return null;
        }

        public CacheEntry GetOrCreate(string key, Func<string> getCallback)
        {
            if (!TryGet(key, out var entry))
            {
                entry = getCallback();
            }
            return entry;
        }

        public CacheEntry Set(string key, string value) => _entries[key] = value;

        public bool TryGet(string key, out CacheEntry entry)
        {
            if (!_entries.TryGetValue(key, out entry))
            {
                return false;
            }

            if (entry.IsExpired)
            {
                _entries.TryRemove(key, out _);
                entry = null;
                return false;
            }
            return true;
        }
    }
}
