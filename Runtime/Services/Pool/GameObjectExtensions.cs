//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using UnityEngine;

namespace BlueCheese.App
{
    public static class GameObjectExtensions 
    {
		public static IGameObjectPool GetPool(this GameObject gameObject)
			=> ServiceLocator.Resolve<IGameObjectPoolService>().GetOrCreatePool(gameObject);

        public static GameObject Spawn(this GameObject gameObject)
			=> GetPool(gameObject).Spawn();

		public static T Spawn<T>(this GameObject gameObject) where T : Component
            => Spawn(gameObject).GetComponent<T>();

        public static void Despawn(this GameObject gameObject, float delay = 0f)
			=> GetPool(gameObject).Despawn(gameObject, delay);

        public static void SetupPool(this GameObject gameObject, PoolOptions options)
		{
			var poolService = ServiceLocator.Resolve<IGameObjectPoolService>();
			poolService.SetupPool(gameObject, options);
		}

        public static void SetupPool<T>(this T component, PoolOptions options) where T : Component
		{
			var poolService = ServiceLocator.Resolve<IGameObjectPoolService>();
			poolService.SetupPool<T>(options);
		}
	}
}
