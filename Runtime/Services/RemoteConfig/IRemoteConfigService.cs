//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App.Services
{
    public interface IRemoteConfigService
    {
        /// <summary>
        /// Returns the values fetched by the remote config provider
        /// </summary>
        Dictionary<string, object> GetValues();

        /// <summary>
        /// Ask the provider to fetch the remote config values
        /// </summary>
        void Fetch();
    }
}
