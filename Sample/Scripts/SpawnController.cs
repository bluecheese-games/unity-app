//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

namespace BlueCheese.App.Sample
{
	public class SpawnController : MonoBehaviour
	{
		[SerializeField] private LocalizedText _counterText;
		[SerializeField] private GameObject _spawnedPrefab;
		[SerializeField] private float _spawnForce = 3f;
		[SerializeField] private float _spawnLifetime = 5f;

		[Injectable] private IPoolService _poolService;
		[Injectable] private IRandomService _random;
		[Injectable] private IClockService _clock;
		[Injectable] private IInputService _input;
		[Injectable] private IAudioService _audio;
		[Injectable] private ILogger<SpawnController> _logger;

		private IPool _pool;

		private void Awake()
		{
			Services.Inject(this);

			_pool = _poolService.Initialize(_spawnedPrefab, new()
			{
				InitialCapacity = 10,
				UseContainer = true,
			});
			_clock.OnTickSecond += Spawn;
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
			_logger.Log("Spawn", this);
			var spawnedInstance = _pool.Spawn();
			spawnedInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
			if (spawnedInstance.TryGetComponent<Rigidbody>(out var rb))
			{
				float angle = _random.Value * Mathf.PI * 2;
				rb.velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnForce;
			}

			_audio.PlaySound("SphereSpawn");

			_pool.Despawn(spawnedInstance, _spawnLifetime);
		}
	}
}
