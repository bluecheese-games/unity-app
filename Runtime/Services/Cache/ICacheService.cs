//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Threading.Tasks;

namespace BlueCheese.App.Services
{
    public interface ICacheService
    {
        bool Exists(string key);

        string Get(string key);
        Task<string> GetAsync(string key);
        bool TryGet(string key, out string value);

        void Set(string key, string value);
        Task SetAsync(string key, string value);

        string GetOrCreate(string key, Func<string> getCallback);
        Task<string> GetOrCreateAsync(string key, Func<Task<string>> getCallback);
    }
}
