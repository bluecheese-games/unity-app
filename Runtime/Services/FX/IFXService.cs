using BlueCheese.Core;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

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
			PrewarmAsync().Forget();
		}

		// Preallocates a pool for every registered FXDef flagged for prewarm. Spread one instance
		// per frame so a startup prewarm list doesn't spike a single frame with many Instantiate calls.
		private async UniTask PrewarmAsync()
		{
			var fxDefs = await AssetBank.GetAssetsOfTypeAsync<FXDef>();
			foreach (var fxDef in fxDefs)
			{
				if (fxDef == null || !fxDef.Prewarm || !fxDef.IsValid)
				{
					continue;
				}

				var options = PoolOptions.Default;
				options.Capacity = Mathf.Max(GameObjectPool.DefaultCapacity, fxDef.PrewarmPoolSize);
				var pool = _poolService.SetupPool(fxDef.Prefab, options);

				for (int count = pool.CountAvailable + pool.CountInUse + 1; count <= fxDef.PrewarmPoolSize; count++)
				{
					pool.Fill(count);
					await UniTask.Yield();
				}
			}
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
