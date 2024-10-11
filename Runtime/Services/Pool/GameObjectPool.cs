//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
	public class GameObjectPool : IGameObjectPool
	{
		private readonly IGameObjectService _gameObjectService;
		private readonly Type _componentType;
		private readonly GameObject _prefab;
		private readonly HashSet<PoolItem> _availableItems = new(8);
		private readonly HashSet<PoolItem> _usedItems = new(8);
		private Transform _container;
		private PoolOptions _options;

		public GameObjectPool(IGameObjectService gameObjectService, GameObject prefab = null, Type componentType = null, PoolOptions options = default)
		{
			_gameObjectService = gameObjectService;
			_prefab = prefab;
			_componentType = componentType;
			_container = null;

			Setup(options);
		}

		public void Setup(PoolOptions options = default) => _ = SetupAsync(options);

		public async Task SetupAsync(PoolOptions options = default)
		{
			_options = options;

			if (options.UseContainer && _container == null)
			{
				_container = _gameObjectService.CreateEmptyObject().transform;
				_container.name = $"Pool<{(_prefab != null ? _prefab.name : _componentType.Name)}>";
				if (options.DontDestroyOnLoad)
				{
					_gameObjectService.DontDestroyOnLoad(_container.gameObject);
				}
			}

			if (options.InitialCapacity > 0)
			{
				_availableItems.EnsureCapacity(options.InitialCapacity);
				_usedItems.EnsureCapacity(options.InitialCapacity);
				for (int i = 0; i < options.InitialCapacity; i++)
				{
					Add(await CreateItemAsync());
				}
			}
		}

		public GameObject Spawn()
		{
			return GetOrCreateItemAsync().Result.gameObject;
		}

		public async Task<GameObject> SpawnAsync()
		{
			var item = await GetOrCreateItemAsync();
			return item.gameObject;
		}

		public T Spawn<T>() where T : Component
		{
			return GetOrCreateItemAsync().Result.GetComponent<T>();
		}

		public async Task<T> SpawnAsync<T>() where T : Component
		{
			var item = await GetOrCreateItemAsync();
			return item.GetComponent<T>();
		}

		private async Task<PoolItem> GetOrCreateItemAsync()
		{
			PoolItem item;
			if (_availableItems.Count > 0)
			{
				item = _availableItems.First();
				item.Recycle();
				item.gameObject.SetActive(true);
				_availableItems.Remove(item);
			}
			else
			{
				item = await CreateItemAsync();
			}
			_usedItems.Add(item);
			return item;
		}

		private async Task<PoolItem> CreateItemAsync()
		{
			GameObject obj = await InstantiateObjectAsync();

			if (_componentType != null)
			{
				obj.AddComponent(_componentType);
				obj.name = $"PoolItem<{_componentType.Name}>";
			}
			if (!obj.TryGetComponent<PoolItem>(out var item))
			{
				item = obj.AddComponent<PoolItem>();
			}
			if (_container != null)
			{
				obj.transform.SetParent(_container);
			}
			else if (_options.DontDestroyOnLoad)
			{
				_gameObjectService.DontDestroyOnLoad(obj);
			}
			item.Pool = this;
			return item;
		}

		private async Task<GameObject> InstantiateObjectAsync()
		{
			if (_options.Factory != null)
			{
				return _options.Factory();
			}
			else if (_options.FactoryAsync != null)
			{
				return await _options.FactoryAsync();
			}
			else if (_prefab != null)
			{
				return await _gameObjectService.InstantiateAsync(_prefab);
			}
			else
			{
				return _gameObjectService.CreateEmptyObject();
			}
		}

		private void Add(PoolItem item)
		{
			item.gameObject.SetActive(false);
			_availableItems.Add(item);
			_usedItems.Remove(item);
		}

		public void Despawn(GameObject obj, float delay = 0f)
		{
			if (!obj.TryGetComponent<PoolItem>(out var item))
			{
				throw new InvalidOperationException("GameObject has no PoolItem component");
			}

			if (delay > 0)
			{
				item.Despawn(delay);
			}
			else
			{
				Add(item);
			}
		}

		public void Despawn<T>(T objectComponent, float delay = 0f) where T : Component
		{
			Despawn(objectComponent.gameObject, delay);
		}

		public void Delete(GameObject obj)
		{
			if (!obj.TryGetComponent<PoolItem>(out var item))
			{
				throw new InvalidOperationException("GameObject has no PoolItem component");
			}

			_availableItems.Remove(item);
			_usedItems.Remove(item);
		}

		public void DespawnAll(float delay = 0f)
		{
			foreach (var item in _availableItems)
			{
				item.GetComponent<PoolItem>().Despawn(delay);
			}
		}

		public void DeleteAll()
		{
			_availableItems.Clear();
		}

		public void Destroy()
		{
			foreach (var item in _availableItems)
			{
				_gameObjectService.Destroy(item.gameObject);
			}
			_availableItems.Clear();

			foreach (var item in _usedItems)
			{
				_gameObjectService.Destroy(item.gameObject);
			}
			_usedItems.Clear();
		}

		public IReadOnlyList<PoolItem> AvailableItems => _availableItems.ToList();

		public IReadOnlyList<PoolItem> UsedItems => _usedItems.ToList();

		public class PoolItem : MonoBehaviour
		{
			internal GameObjectPool Pool { get; set; }
			private IRecyclable[] _recyclables;
			private Coroutine _despawnCoroutine;

			private void Awake()
			{
				_recyclables = GetComponents<IRecyclable>();
			}

			public void Recycle()
			{
				if (_despawnCoroutine != null)
				{
					StopCoroutine(_despawnCoroutine);
				}
				foreach (var recyclable in _recyclables)
				{
					recyclable.Recycle();
				}
			}

			public void Despawn(float delay)
			{
				if (delay <= 0f)
				{
					Despawn();
				}
				else
				{
					if (_despawnCoroutine != null)
					{
						StopCoroutine(_despawnCoroutine);
					}
					_despawnCoroutine = StartCoroutine(DespawnRoutine(delay));
				}
			}

			private IEnumerator DespawnRoutine(float delay)
			{
				yield return new WaitForSeconds(delay);
				Despawn();
			}

			public void Despawn()
			{
				Pool.Add(this);
			}

			private void OnDestroy()
			{
				Pool.Delete(gameObject);
			}
		}
	}

}
