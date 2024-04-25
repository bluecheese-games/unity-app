//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using BlueCheese.Unity.Core.Services;

namespace BlueCheese.Unity.App.Services
{
    public interface IConfigService : IInitializable
    {
        Config Config { get; }
    }
}
