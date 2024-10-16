//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
    public interface IRemoteConfigService
    {
        /// <summary>
        /// Returns the values fetched by the remote config provider
        /// </summary>
        IReadOnlyDictionary<string, object> GetValues();

		/// <summary>
		/// Ask the provider to fetch the remote config values
		/// </summary>
		UniTask FetchAsync();
    }
}
