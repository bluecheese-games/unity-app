//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public interface IGameObjectPoolService
    {
		/// <summary>
		/// Initializes the pool with the given prefab and options.
		/// </summary>
		IGameObjectPool SetupPool(GameObject prefab, PoolOptions options = default);

		/// <summary>
		/// Initializes the pool with the given component type and options.
		/// </summary>
		IGameObjectPool SetupPool<T>(PoolOptions options = default) where T : Component;

		/// <summary>
		/// Gets or creates a pool for the given prefab.
		/// </summary>
		IGameObjectPool GetOrCreatePool(GameObject prefab);

		/// <summary>
		/// Gets or creates a pool for the given component type.
		/// </summary>
		IGameObjectPool GetOrCreatePool<T>() where T : Component;
	}
}
