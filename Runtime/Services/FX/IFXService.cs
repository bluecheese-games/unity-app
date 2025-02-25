//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using BlueCheese.Core.ServiceLocator;
using System;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface IFXService : IInitializable, IDisposable
	{
		FXInstance CreateFX(FXDef fxDef);

		void RegisterInstance(FXInstance instance);
	}

	public class FXService : IFXService
	{
		private readonly IGameObjectPoolService _poolService;
		private readonly IClockService _clock;

		private List<FXInstance> _instances = new();
		private List<FXInstance> _instancesToRemove = new();

		public FXService(IGameObjectPoolService poolService, IClockService clock)
		{
			_poolService = poolService;
			_clock = clock;
		}

		public void Initialize()
		{
			_clock.OnTick += UpdateInstances;
		}

		private void UpdateInstances(float deltaTime)
		{
			for (int i = 0; i < _instances.Count; i++)
			{
				_instances[i].UpdateFX(deltaTime);

				if (!_instances[i].IsAlive)
				{
					_instancesToRemove.Add(_instances[i]);
				}
			}

			if (_instancesToRemove.Count > 0)
			{
				CleanupInstances();
			}
		}

		private void CleanupInstances()
		{
			for (int i = 0; i < _instancesToRemove.Count; i++)
			{
				if (_instancesToRemove[i].TryGetComponent<GameObjectPool.PoolItem>(out var poolItem))
				{
					poolItem.Despawn();
				}
				_instances.Remove(_instancesToRemove[i]);
			}
			_instancesToRemove.Clear();
		}

		public FXInstance CreateFX(FXDef fxDef)
		{
			var obj = _poolService.GetOrCreatePool(fxDef.Prefab).Spawn();
			var instance = obj.AddOrGetComponent<FXInstance>();
			instance.Setup(fxDef);
			RegisterInstance(instance);
			return instance;
		}

		public void RegisterInstance(FXInstance instance)
		{
			if (!_instances.Contains(instance))
			{
				_instances.Add(instance);
			}
		}

		public void Dispose()
		{
			_clock.OnTick -= UpdateInstances;
			_instances.Clear();
			_instancesToRemove.Clear();
		}
	}
}
