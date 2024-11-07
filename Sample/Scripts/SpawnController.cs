//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App.Sample
{
	public class SpawnController : MonoBehaviour
	{
		[SerializeField] private LocalizedText _counterText;
		[SerializeField] private GameObject _spawnedPrefab;
		[SerializeField] private float _spawnForce = 3f;
		[SerializeField] private float _spawnLifetime = 5f;
		[SerializeField] private SoundFX _spawnSFX = "SphereSpawn";

		[Injectable] private IGameObjectPoolService _poolService;
		[Injectable] private IRandomService _random;
		[Injectable] private IInputService _input;
		[Injectable] private IAudioService _audio;
		[Injectable] private ILogger<SpawnController> _logger;

		private IGameObjectPool _pool;

		private void Awake()
		{
			Services.Inject(this);

			_pool = _poolService.SetupPool(_spawnedPrefab, new()
			{
				Capacity = 15,
				Overflow = PoolOverflow.LogError,
				FillAmount = 10,
				UseContainer = true,
			});

			InvokeRepeating(nameof(Spawn), 1f, 1f);
		}

		private void Update()
		{
			if (_input.GetButtonDown("Jump"))
			{
				Spawn();
			}
			if (_input.GetButtonDown("Fire2"))
			{
				_pool.Destroy();
			}

			_counterText.SetParameter(0, _pool.UsedItems.Count.ToString());
		}

		private void Spawn()
		{
			Log.Debug("Spawn", this);
			var spawnedInstance = _pool.Spawn();
			spawnedInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
			if (spawnedInstance.TryGetComponent<Rigidbody>(out var rb))
			{
				float angle = _random.Value * Mathf.PI * 2;
				rb.velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnForce;
			}

			_spawnSFX.Play(transform.position);

			_pool.Despawn(spawnedInstance, _spawnLifetime);
		}
	}
}
