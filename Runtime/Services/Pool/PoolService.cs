//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	public class PoolService : IPoolService
	{
		private readonly Dictionary<int, GameObjectPool> _pools = new();
		private readonly IGameObjectService _gameObjectService;

		public PoolService(IGameObjectService gameObjectService)
		{
			_gameObjectService = gameObjectService;
		}

		public IPool Initialize(GameObject prefab, PoolOptions options = default)
			=> GetOrCreatePool(prefab.GetHashCode(), prefab: prefab, options: options);

		public IPool Initialize<T>(PoolOptions options = default) where T : Component
			=> GetOrCreatePool(typeof(T).GetHashCode(), componentType: typeof(T), options: options);

		public IPool GetOrCreatePool(GameObject prefab)
			=> GetOrCreatePool(prefab.GetHashCode(), prefab: prefab);

		public IPool GetOrCreatePool<T>() where T : Component
			=> GetOrCreatePool(typeof(T).GetHashCode(), componentType: typeof(T));

		private GameObjectPool GetOrCreatePool(int id, GameObject prefab = null, Type componentType = null, PoolOptions options = default)
		{
			if (_pools.TryGetValue(id, out var pool))
			{
				return pool;
			}
			pool = new GameObjectPool(_gameObjectService, prefab, componentType, options);
			_pools.Add(id, pool);
			return pool;
		}
	}

}
