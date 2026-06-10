using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using BlueCheese.Core.DI;

namespace BlueCheese.App
{
	[Serializable]
	public class Pool<T> where T : UnityEngine.Object
	{
		[SerializeField] private T _prefab;

		private IGameObjectPool _internalPool;
		private bool _initialized;

		public T Spawn(float lifetime = -1f)
		{
			if (!_initialized) Initialize();

			var go = _internalPool.Spawn();
			if (go != null && lifetime > 0f)
			{
				_internalPool.Despawn(go, lifetime);
			}

			if (typeof(T) == typeof(GameObject))
			{
				return go as T;
			}

			if (go.TryGetComponent<T>(out var component))
			{
				return component;
			}
			return null;
		}

		public void Despawn(T instance, float delay = 0f)
		{
			if (!_initialized) return;

			if (instance is GameObject go)
			{
				_internalPool.Despawn(go, delay);
			}
			else if (instance is Component comp)
			{
				_internalPool.Despawn(comp.gameObject, delay);
			}
		}

		private void Initialize()
		{
			var service = ServiceLocator.Resolve<IGameObjectPoolService>();

			GameObject prefabGo = _prefab as GameObject;
			if (prefabGo == null && _prefab is Component comp)
			{
				prefabGo = comp.gameObject;
			}

			_internalPool = service.GetOrCreatePool(prefabGo);
			_internalPool.Setup(FetchOptions());
			_initialized = true;
		}

		private PoolOptions FetchOptions()
		{
			StackTrace stackTrace = new StackTrace(true);

			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				var frame = stackTrace.GetFrame(i);
				var method = frame.GetMethod();
				var type = method?.DeclaringType;

				if (type == null || type == typeof(Pool<T>) || typeof(IGameObjectPool).IsAssignableFrom(type))
					continue;

				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				foreach (var field in fields)
				{
					if (field.FieldType != typeof(Pool<T>)) continue;

					var attr = field.GetCustomAttribute<PoolOptionsAttribute>();
					if (attr != null) return attr.ToPoolOptions();
				}
			}

			return new PoolOptions { Capacity = 10 };
		}
	}
}