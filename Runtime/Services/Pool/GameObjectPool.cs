//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
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
		private readonly HashSet<PoolItem> _inactiveItems = new(DefaultCapacity);
		private readonly HashSet<PoolItem> _activeItems = new(DefaultCapacity);
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
			if (_options.Capacity <= 0)
			{
				_options.Capacity = DefaultCapacity;
			}

			if (options.UseContainer && _container == null)
			{
				_container = _gameObjectService.CreateEmptyObject().transform;
				_container.name = $"Pool<{(_prefab != null ? _prefab.name : _componentType.Name)}>";
			}

			if (_container != null && options.DontDestroyOnLoad)
			{
				_gameObjectService.DontDestroyOnLoad(_container.gameObject);
			}

			_inactiveItems.EnsureCapacity(options.Capacity);
			_activeItems.EnsureCapacity(options.Capacity);

			Fill(options.FillAmount);
		}

		public void Fill(int amount)
		{
			if (amount <= 0)
			{
				return;
			}

			for (int i = _inactiveItems.Count + _activeItems.Count; i < amount; i++)
			{
				Add(CreateItem(false));
			}
		}

		public GameObject Spawn()
		{
			var item = GetOrCreateItem();
			return item != null ? item.gameObject : null;
		}

		public T Spawn<T>() where T : Component
		{
			var item = GetOrCreateItem();
			return item != null ? item.GetComponent<T>() : null;
		}

		private PoolItem GetOrCreateItem()
		{
			PoolItem item;
			if (_inactiveItems.Count > 0)
			{
				item = _inactiveItems.First();
				_inactiveItems.Remove(item);

				item.Recycle();
				if (!item.gameObject.activeSelf)
				{
					item.gameObject.SetActive(true);
				}
			}
			else
			{
				item = CreateItem(true);
			}

			if (item != null)
			{
				_activeItems.Add(item);
			}
			return item;
		}

		private PoolItem CreateItem(bool enabled)
		{
			int count = _inactiveItems.Count + _activeItems.Count;
			if (count >= _options.Capacity)
			{
				switch (_options.Overflow)
				{
					case PoolOverflow.Ignore:
						break;
					case PoolOverflow.LogError:
						_logger.LogError($"Pool<{(_prefab != null ? _prefab.name : _componentType.Name)}> overflows capacity ({_options.Capacity})");
						break;
					case PoolOverflow.RecycleActive:
						var oldest = _activeItems.First();
						Despawn(oldest.gameObject);
						return GetOrCreateItem();
					case PoolOverflow.ReturnsNull:
						return null;
				}
			}

			GameObject obj = InstantiateObject(enabled);

			if (_componentType != null)
			{
				if (obj.GetComponent(_componentType) == null)
				{
					obj.AddComponent(_componentType);
				}
				obj.name = $"PoolItem<{_componentType.Name}>";
			}

			var item = obj.AddOrGetComponent<PoolItem>();
			item.Pool = this;
			item.hideFlags = HideFlags.HideInInspector;

			if (_container != null)
			{
				obj.transform.SetParent(_container);
			}
			else if (_options.DontDestroyOnLoad)
			{
				_gameObjectService.DontDestroyOnLoad(obj);
			}

			return item;
		}

		private GameObject InstantiateObject(bool enabled)
		{
			GameObject obj;
			if (_options.Factory != null)
			{
				obj = _options.Factory();
			}
			else if (_prefab != null)
			{
				if (!enabled)
				{
					_prefab.SetActive(false);
				}
				obj = _gameObjectService.Instantiate(_prefab);
			}
			else
			{
				obj = _gameObjectService.CreateEmptyObject(name: $"PoolItem<{(_componentType != null ? _componentType.Name : "Empty")}>");
			}

			obj.SetActive(enabled);
			return obj;
		}

		private void Add(PoolItem item)
		{
			if (item == null) return;

			if (item.gameObject.activeSelf)
			{
				item.gameObject.SetActive(false);
			}

			_activeItems.Remove(item);

			int count = _inactiveItems.Count + _activeItems.Count;
			if (count >= _options.Capacity && _options.Overflow != PoolOverflow.Ignore)
			{
				_gameObjectService.Destroy(item.gameObject);
			}
			else
			{
				_inactiveItems.Add(item);
			}
		}

		public void Despawn(GameObject obj, float delay = 0f)
		{
			if (obj == null) return;

			if (!obj.TryGetComponent<PoolItem>(out var item))
			{
				_logger.LogError($"GameObject {obj.name} has no PoolItem component and cannot be despawned to this pool.");
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
			if (objectComponent != null)
			{
				Despawn(objectComponent.gameObject, delay);
			}
		}

		public void Delete(GameObject obj)
		{
			if (obj != null && obj.TryGetComponent<PoolItem>(out var item))
			{
				_inactiveItems.Remove(item);
				_activeItems.Remove(item);
			}
		}

		public void DespawnAll(float delay = 0f)
		{
			var itemsToDespawn = _activeItems.ToList();
			foreach (var item in itemsToDespawn)
			{
				item.Despawn(delay);
			}
		}

		public void DeleteAll()
		{
			var allItems = _inactiveItems.Concat(_activeItems).ToList();
			foreach (var item in allItems)
			{
				if (item != null && item.gameObject != null)
				{
					_gameObjectService.Destroy(item.gameObject);
				}
			}
			_inactiveItems.Clear();
			_activeItems.Clear();
		}

		public void Destroy()
		{
			DeleteAll();

			if (_container != null)
			{
				_gameObjectService.Destroy(_container.gameObject);
				_container = null;
			}
		}

		public int CountInUse => _activeItems.Count;
		public int CountAvailable => _inactiveItems.Count;

		public class PoolItem : MonoBehaviour
		{
			internal GameObjectPool Pool { get; set; }
			private Coroutine _despawnCoroutine;

			public void Recycle()
			{
				if (_despawnCoroutine != null)
				{
					StopCoroutine(_despawnCoroutine);
					_despawnCoroutine = null;
				}

				var recyclables = GetComponents<IRecyclable>();
				for (int i = 0; i < recyclables.Length; i++)
				{
					recyclables[i].OnRecycle();
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
				_despawnCoroutine = null;
				Despawn();
			}

			public void Despawn()
			{
				var despawnables = GetComponents<IDespawnable>();
				for (int i = 0; i < despawnables.Length; i++)
				{
					despawnables[i].OnDespawn();
				}
				Pool.Add(this);
			}

			private void OnDestroy()
			{
				if (Pool != null)
				{
					Pool.Delete(gameObject);
				}
			}
		}
	}
}