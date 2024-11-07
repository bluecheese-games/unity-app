//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App
{
    public static class GameObjectExtensions 
    {
        public static GameObject Spawn(this GameObject gameObject)
        {
            var poolService = Services.Get<IGameObjectPoolService>();
            var pool = poolService.GetOrCreatePool(gameObject);
            return pool.Spawn();
        }

		public static T Spawn<T>(this GameObject gameObject) where T : Component
            => Spawn(gameObject).GetComponent<T>();

        public static void Despawn(this GameObject gameObject, float delay = 0f)
		{
			var poolService = Services.Get<IGameObjectPoolService>();
			var pool = poolService.GetOrCreatePool(gameObject);
            pool.Despawn(gameObject, delay);
		}

        public static void SetupPool(this GameObject gameObject, PoolOptions options)
		{
			var poolService = Services.Get<IGameObjectPoolService>();
			poolService.SetupPool(gameObject, options);
		}

        public static void SetupPool<T>(this T component, PoolOptions options) where T : Component
		{
			var poolService = Services.Get<IGameObjectPoolService>();
			poolService.SetupPool<T>(options);
		}
	}
}
