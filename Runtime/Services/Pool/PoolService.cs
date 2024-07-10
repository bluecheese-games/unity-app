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
		private readonly Dictionary<int, Pool> _pools = new();
		private readonly IGameObjectService _gameObjectService;
		private readonly IClockService _clockService;

		public PoolService(IGameObjectService gameObjectService, IClockService clockService)
		{
			_gameObjectService = gameObjectService;
			_clockService = clockService;
		}

		public void Initialize(GameObject prefab, PoolOptions options = default)
			=> GetOrCreatePool(prefab.GetHashCode(), prefab: prefab, options: options);

		public void Initialize<T>(PoolOptions options = default) where T : Component
			=> GetOrCreatePool(typeof(T).GetHashCode(), componentType: typeof(T), options: options);

		public T Spawn<T>() where T : Component => GetOrCreatePool<T>().Spawn().GetComponent<T>();

		public GameObject Spawn(GameObject prefab) => GetOrCreatePool(prefab).Spawn();

		public void Despawn(GameObject instance, float delay = 0f)
		{
			_clockService.InvokeAsync(() => DespawnInstance(instance), delay);
		}

		public void Remove(GameObject instance)
		{
			if (instance.TryGetComponent<PoolInstance>(out var poolInstance))
			{
				poolInstance.Pool.Remove(instance);
			}
		}

		private void DespawnInstance(GameObject instance)
		{
			if (instance.TryGetComponent<PoolInstance>(out var poolInstance))
			{
				instance.SetActive(false);
				poolInstance.Pool.Despawn(instance);
			}
		}

		private Pool GetOrCreatePool(GameObject prefab)
			=> GetOrCreatePool(prefab.GetHashCode(), prefab: prefab);

		private Pool GetOrCreatePool<T>() where T : Component
			=> GetOrCreatePool(typeof(T).GetHashCode(), componentType: typeof(T));

		private Pool GetOrCreatePool(int id, GameObject prefab = null, Type componentType = null, PoolOptions options = default)
		{
			if (_pools.TryGetValue(id, out var pool))
			{
				return pool;
			}
			pool = new Pool(_gameObjectService, prefab, componentType, options);
			_pools.Add(id, pool);
			return pool;
		}

		internal class Pool
		{
			private readonly IGameObjectService _gameObjectService;
			private readonly Type _componentType;
			private readonly GameObject _prefab;
			private readonly List<GameObject> _items = new();
			private readonly Transform _container;
			private readonly PoolOptions _options;

			public Pool(IGameObjectService gameObjectService, GameObject prefab = null, Type componentType = null, PoolOptions options = default)
			{
				_gameObjectService = gameObjectService;
				_prefab = prefab;
				_componentType = componentType;
				_container = options.UseContainer ? _gameObjectService.CreateEmptyObject().transform : null;
				_options = options;
				if (_container != null && options.DontDestroyOnLoad)
				{
					_gameObjectService.DontDestroyOnLoad(_container.gameObject);
				}

				if (_container != null)
				{
					_container.name = $"Pool<{(prefab != null ? prefab.name : _componentType.Name)}>";
				}

				if (options.InitialCapacity > 0)
				{
					for (int i = 0; i < options.InitialCapacity; i++)
					{
						Despawn(SpawnNewInstance());
					}
				}
			}

			public GameObject Spawn()
			{
				for (int i = 0; i < _items.Count; i++)
				{
					if (!_items[i].activeSelf)
					{
						var recycledInstance = _items[i];
						recycledInstance.GetComponent<PoolInstance>().Recycle();
						recycledInstance.SetActive(true);
						_items.RemoveAt(i);
						return recycledInstance;
					}
				}
				return SpawnNewInstance();
			}

			private GameObject SpawnNewInstance()
			{
				GameObject instance = CreateInstance();

				if (_componentType != null)
				{
					instance.AddComponent(_componentType);
					instance.name = $"PoolItem<{_componentType.Name}>";
				}
				if (!instance.TryGetComponent<PoolInstance>(out var poolInstance))
				{
					poolInstance = instance.AddComponent<PoolInstance>();
				}
				if (_container != null)
				{
					instance.transform.SetParent(_container);
				}
				else if (_options.DontDestroyOnLoad)
				{
					_gameObjectService.DontDestroyOnLoad(instance);
				}
				poolInstance.Pool = this;
				return instance;
			}

			private GameObject CreateInstance()
			{
				if (_options.Factory != null)
				{
					return _options.Factory();
				}
				else if (_prefab != null)
				{
					return _gameObjectService.Instantiate(_prefab);
				}
				else
				{
					return _gameObjectService.CreateEmptyObject();
				}
			}

			public void Despawn(GameObject instance)
			{
				instance.SetActive(false);
				_items.Add(instance);
			}

			public void Remove(GameObject instance)
			{
				if (_items.Contains(instance))
				{
					_items.Remove(instance);
				}
			}

			public int Count => _items.Count;
		}
	}

	public class PoolInstance : MonoBehaviour
	{
		internal PoolService.Pool Pool { get; set; }
		private IRecyclable[] _recyclables;

		private void Awake()
		{
			_recyclables = GetComponents<IRecyclable>();
		}

		public void Recycle()
		{
			foreach (var recyclable in _recyclables)
			{
				recyclable.Recycle();
			}
		}

		private void OnDestroy()
		{
			Pool.Remove(gameObject);
		}
	}

}
