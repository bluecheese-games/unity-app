//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using BlueCheese.Core.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BlueCheese.App.Sample
{
	public class SpawnController : MonoBehaviour
	{
		[SerializeField] private LocalizedText _counterText;
		[SerializeField] private PrefabCollection _spawnedPrefabCollection;
		[SerializeField] private float _spawnInterval = 1f;
		[SerializeField] private float _spawnForce = 3f;
		[SerializeField] private float _spawnLifetime = 5f;
		[SerializeField] private SoundFX _spawnSFX = "SphereSpawn";

		[Injectable] private IGameObjectPoolService _poolService;
		[Injectable] private IRandomService _random;
		[Injectable] private IInputService _input;
		[Injectable] private IAudioService _audio;
		[Injectable] private ILogger<SpawnController> _logger;

		private HashSet<IGameObjectPool> _pools = new();

		private void Awake()
		{
			Services.Inject(this);
			Assert.IsNotNull(_poolService);
		}

		private void OnEnable()
		{
			InvokeRepeating(nameof(Spawn), 1f, _spawnInterval);
		}

		private void OnDisable()
		{
			CancelInvoke(nameof(Spawn));
		}

		private void Update()
		{
			if (_input.GetButtonDown("Jump"))
			{
				Spawn();
			}

			int usedItems = 0;
			foreach (var pool in _pools)
			{
				usedItems += pool.CountInUse;
			}
			_counterText.SetParameter(0, usedItems.ToString());
		}

		private void Spawn()
		{
			Log.Debug("Spawn", this);
			var prefab = _spawnedPrefabCollection.GetRandom();
			var pool = prefab.GetPool();

			if (!_pools.Contains(pool))
			{
				_pools.Add(pool);
				pool.Setup(new()
				{
					FillAmount = 10,
					UseContainer = true,
				});
			}

			var spawnedInstance = pool.Spawn();
			spawnedInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
			if (spawnedInstance.TryGetComponent<Rigidbody>(out var rb))
			{
				float angle = _random.Value * Mathf.PI * 2;
				rb.velocity = new Vector3(Mathf.Cos(angle) * 0.5f, 1f, Mathf.Sin(angle) * 0.5f) * _spawnForce;
			}

			_spawnSFX.Play();

			pool.Despawn(spawnedInstance, _spawnLifetime);
		}
	}
}
