//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App.Services
{
    public class DefaultRemoteConfigService : IRemoteConfigService
    {
        public Dictionary<string, object> GetValues() => new();

        public void Fetch() { }
    }
}
