//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
    public class DefaultRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, object> _values = new();

        public IReadOnlyDictionary<string, object> GetValues() => _values;

        public async UniTask FetchAsync() => await UniTask.CompletedTask;
    }
}
