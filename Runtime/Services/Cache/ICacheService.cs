//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.App
{
    public interface ICacheService
    {
        bool Exists(string key);

        CacheEntry Get(string key);

        bool TryGet(string key, out CacheEntry entry);

        CacheEntry Set(string key, string value);

        CacheEntry GetOrCreate(string key, Func<string> getCallback);

        IReadOnlyDictionary<string, CacheEntry> GetEntries();
    }
}
