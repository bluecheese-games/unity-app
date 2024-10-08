//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public interface IPoolService
    {

		/// <summary>
		/// Initializes the pool with the given prefab and options.
		/// </summary>
		IPool Initialize(GameObject prefab, PoolOptions options = default);

		/// <summary>
		/// Initializes the pool with the given component type and options.
		/// </summary>
		IPool Initialize<T>(PoolOptions options = default) where T : Component;

		/// <summary>
		/// Gets or creates a pool for the given prefab.
		/// </summary>
		IPool GetOrCreatePool(GameObject prefab);

		/// <summary>
		/// Gets or creates a pool for the given component type.
		/// </summary>
		IPool GetOrCreatePool<T>() where T : Component;
	}
}
