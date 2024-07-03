//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueCheese.App
{
    public class DefaultRemoteConfigService : IRemoteConfigService
    {
        public Dictionary<string, object> GetValues() => new();

        public async Task FetchAsync() => await Task.CompletedTask;
    }
}
