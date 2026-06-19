//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Config;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BlueCheese.App.Sample
{
	[Serializable]
	public enum SampleEnum { OptionA, OptionB, OptionC, OptionD }

	public class SampleGameController : MonoBehaviour
	{
		[SerializeField] private FlagEnum<SampleEnum> _sampleFlagEnum;
		[SerializeField, ButtonsEnum] private SampleEnum _buttonsEnum;
		[SerializeField, SearchableEnum] private MyEnumWrapper _testSearchableEnum;
		[SerializeField] private LocalizedText _counterText;
		[SerializeField] private AssetRef<PrefabCollection> _spawnedPrefabs;
		[SerializeField] private AssetRef<ConfigAsset> _config;
		[SerializeField] private float _spawnInterval = 1f;
		[SerializeField] private float _spawnForce = 3f;
		[SerializeField] private float _spawnLifetime = 5f;
		[SerializeField] private SoundFX _spawnSFX = "SphereSpawn";
		[PoolOptions(Capacity = 20, FillAmount = 10, UseContainer = true)]
		[SerializeField] private Pool<GameObject> _testPool;

		[Injectable] private IGameObjectPoolService _poolService;
		[Injectable] private IRandomService _random;
		[Injectable] private IInputService _input;

		private HashSet<IGameObjectPool> _pools = new();

		private void Awake()
		{
			ServiceInjector.Inject(this);
			Assert.IsNotNull(_poolService);

			DevMetricRuntime.Record("SpawnController_Awake", Time.realtimeSinceStartup);
		}

		private void OnEnable()
		{
			//InvokeRepeating(nameof(Spawn), 1f, _spawnInterval);

			Debug.Log(_config.Asset.Get<string>("TestKey", "DefaultValue"));
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
			_testPool.Spawn(5f);
			return;

			Log.Debug("Spawn", this);
			var prefab = _spawnedPrefabs.Asset.GetRandom();
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
				rb.linearVelocity = new Vector3(Mathf.Cos(angle) * 0.5f, 1f, Mathf.Sin(angle) * 0.5f) * _spawnForce;
			}

			_spawnSFX.Play();

			pool.Despawn(spawnedInstance, _spawnLifetime);
		}
	}

	[Serializable]
	public class MyEnumWrapper
	{
		[SerializeField] private MyEnum value;

		public MyEnum Value => value;

		public static implicit operator MyEnum(MyEnumWrapper w) => w != null ? w.value : default;
	}


	[Serializable]
	public enum MyEnum
	{
		Value0,
		Value1,
		Value2,
		Value3,
		Value4,
		Value5,
		Value6,
		Value7,
		Value8,
		Value9,
		Value10,
		Value11,
		Value12,
		Value13,
		Value14,
		Value15,
		Value16,
		Value17,
		Value18,
		Value19,
		Value20,
		Value21,
		Value22,
		Value23,
		Value24,
		Value25,
		Value26,
		Value27,
		Value28,
		Value29,
		Value30,
		Value31,
		Value32,
		Value33,
		Value34,
		Value35,
		Value36,
		Value37,
		Value38,
		Value39,
		Value40,
		Value41,
		Value42,
		Value43,
		Value44,
		Value45,
		Value46,
		Value47,
		Value48,
		Value49,
		Value50,
		Value51,
		Value52,
		Value53,
		Value54,
		Value55,
		Value56,
		Value57,
		Value58,
		Value59,
		Value60,
		Value61,
		Value62,
		Value63,
		Value64,
		Value65,
		Value66,
		Value67,
		Value68,
	}
}
