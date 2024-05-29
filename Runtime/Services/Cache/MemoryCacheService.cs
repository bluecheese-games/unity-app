//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BlueCheese.App.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, string> _items = new();

        public bool Exists(string key) => _items.ContainsKey(key);

        public string Get(string key) => _items.TryGetValue(key, out var value) ? value : null;

        public Task<string> GetAsync(string key) => Task.FromResult(Get(key));

        public string GetOrCreate(string key, Func<string> getCallback) => _items.GetOrAdd(key, _ => getCallback());

        public async Task<string> GetOrCreateAsync(string key, Func<Task<string>> createCallback)
        {
            if (_items.TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }

            string newValue = await createCallback();

            return _items.GetOrAdd(key, newValue);
        }

        public void Set(string key, string value) => _items[key] = value;

        public Task SetAsync(string key, string value)
        {
            Set(key, value);
            return Task.CompletedTask;
        }

        public bool TryGet(string key, out string value) => _items.TryGetValue(key, out value);
    }
}
