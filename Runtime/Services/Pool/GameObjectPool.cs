//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	public class GameObjectPool : IGameObjectPool
	{
		public const int DefaultCapacity = 10;

		private readonly IGameObjectService _gameObjectService;
		private readonly ILogger<GameObjectPoolService> _logger;
		private readonly Type _componentType;
		private readonly GameObject _prefab;
		private readonly HashSet<PoolItem> _poolItems = new(DefaultCapacity);
		private readonly HashSet<PoolItem> _usedItems = new(DefaultCapacity);
		private Transform _container;
		private PoolOptions _options;

		public GameObjectPool(IGameObjectService gameObjectService, ILogger<GameObjectPoolService> logger, GameObject prefab = null, Type componentType = null, PoolOptions options = default)
		{
			_gameObjectService = gameObjectService;
			_logger = logger;
			_prefab = prefab;
			_componentType = componentType;
			_container = null;

			Setup(options);
		}

		public void Setup(PoolOptions options = default)
		{
			_options = options;

			if (options.UseContainer && _container == null)
			{
				_container = _gameObjectService.CreateEmptyObject().transform;
				_container.name = $"Pool<{(_prefab != null ? _prefab.name : _componentType.Name)}>";
			}

			if (_container != null && options.DontDestroyOnLoad)
			{
				_gameObjectService.DontDestroyOnLoad(_container.gameObject);
			}

			_poolItems.EnsureCapacity(options.Capacity);
			_usedItems.EnsureCapacity(options.Capacity);

			Fill(options.FillAmount);
		}

		/// <summary>
		/// Fill the pool with amount of instances.
		/// </summary>
		public void Fill(int amount)
		{
			if (amount <= 0)
			{
				return;
			}

			for (int i = _poolItems.Count; i < amount; i++)
			{
				Add(CreateItem());
			}
		}

		public GameObject Spawn()
		{
			var item = GetOrCreateItem();
			if (item == null)
			{
				return null;
			}
			return item.gameObject;
		}

		public T Spawn<T>() where T : Component
		{
			var item = GetOrCreateItem();
			if (item == null)
			{
				return null;
			}
			return item.GetComponent<T>();
		}

		private PoolItem GetOrCreateItem()
		{
			PoolItem item;
			if (_poolItems.Count > 0)
			{
				item = _poolItems.First();
				item.Recycle();
				item.gameObject.SetActive(true);
				_poolItems.Remove(item);
			}
			else
			{
				item = CreateItem();
			}
			_usedItems.Add(item);
			return item;
		}

		private PoolItem CreateItem()
		{
			int count = _poolItems.Count + _usedItems.Count;
			if (count >= _options.Capacity)
			{
				switch (_options.Overflow)
				{
					case PoolOverflow.Force:
						break;
					case PoolOverflow.LogError:
						if (count == _options.Capacity)
						{
							_logger.LogError($"Pool<{(_prefab != null ? _prefab.name : _componentType.Name)}> overflows capacity ({_options.Capacity})");
						}
						break;
					case PoolOverflow.RecycleActive:
						_usedItems.First().Despawn();
						break;
					case PoolOverflow.ReturnsNull:
						return null;
				}
			}

			GameObject obj = InstantiateObject();

			if (_componentType != null)
			{
				obj.AddComponent(_componentType);
				obj.name = $"PoolItem<{_componentType.Name}>";
			}
			var item = obj.AddOrGetComponent<PoolItem>();
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

		private GameObject InstantiateObject()
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

		private void Add(PoolItem item)
		{
			int count = _poolItems.Count + _usedItems.Count;
			if (count >= _options.Capacity)
			{
				GameObject.Destroy(item.gameObject);
			}
			else
			{
				item.gameObject.SetActive(false);
				_poolItems.Add(item);
				_usedItems.Remove(item);
			}
		}

		public void Despawn(GameObject obj, float delay = 0f)
		{
			if (!obj.TryGetComponent<PoolItem>(out var item))
			{
				_logger.LogError("GameObject has no PoolItem component");
				return;
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
				_logger.LogError("GameObject has no PoolItem component");
				return;
			}

			_poolItems.Remove(item);
			_usedItems.Remove(item);
		}

		public void DespawnAll(float delay = 0f)
		{
			foreach (var item in _poolItems)
			{
				item.GetComponent<PoolItem>().Despawn(delay);
			}
		}

		public void DeleteAll()
		{
			_poolItems.Clear();
		}

		public void Destroy()
		{
			foreach (var item in _poolItems)
			{
				_gameObjectService.Destroy(item.gameObject);
			}
			_poolItems.Clear();

			foreach (var item in _usedItems)
			{
				_gameObjectService.Destroy(item.gameObject);
			}
			_usedItems.Clear();

			if (_container != null)
			{
				_gameObjectService.Destroy(_container.gameObject);
				_container = null;
			}
		}

		public IReadOnlyList<PoolItem> PoolItems => _poolItems.ToList();

		public IReadOnlyList<PoolItem> UsedItems => _usedItems.ToList();

		public class PoolItem : MonoBehaviour
		{
			internal GameObjectPool Pool { get; set; }
			private Coroutine _despawnCoroutine;

			public void Recycle()
			{
				if (_despawnCoroutine != null)
				{
					StopCoroutine(_despawnCoroutine);
				}
				foreach (var recyclable in GetComponents<IRecyclable>())
				{
					recyclable.OnRecycle();
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
				foreach (var despawnable in GetComponents<IDespawnable>())
				{
					despawnable.OnDespawn();
				}
				Pool.Add(this);
			}

			private void OnDestroy()
			{
				Pool.Delete(gameObject);
			}
		}
	}

}
