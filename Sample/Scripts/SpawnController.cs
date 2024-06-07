//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App.Services;
using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App.Sample
{
    public class SpawnController : MonoBehaviour
    {
        [SerializeField] private GameObject _spawnedPrefab;
        [SerializeField] private float _spawnForce = 3f;
        [SerializeField] private float _spawnLifetime = 5f;

        [Injectable] private IPoolService _pool;
        [Injectable] private IRandomService _random;
        [Injectable] private IClockService _clock;
        [Injectable] private IInputService _input;
        [Injectable] private ILogger<SpawnController> _logger;

        private void Awake()
        {
            ServiceContainer.Default.Inject(this);

            _pool.Initialize(_spawnedPrefab, new()
            {
                InitialCapacity = 10
            });
            _clock.OnTickSecond += Spawn;
        }

        private void Update()
        {
            if (_input.GetButtonDown("Fire1"))
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            _logger.Log("Spawn", this);
            var spawnedInstance = _pool.Spawn(_spawnedPrefab);
            spawnedInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
            if (spawnedInstance.TryGetComponent<Rigidbody>(out var rb))
            {
                float angle = _random.Value * Mathf.PI * 2;
                rb.velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnForce;
            }

            _pool.Despawn(spawnedInstance, _spawnLifetime);
        }
    }
}
