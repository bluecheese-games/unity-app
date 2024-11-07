//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_AudioService
	{
		private AudioService _audioService;

		[SetUp]
		public void SetUp()
		{
			var localStorage = new FakeLocalStorageService();
			var gameObjectService = new FakeGameObjectService();
			var logger = new FakeLogger<GameObjectPoolService>();
			var pool = new GameObjectPoolService(gameObjectService, logger);
			var assetLoader = new FakeAssetLoaderService();
			var audioBank = ScriptableObject.CreateInstance<AudioBank>();
			audioBank.Items = new List<AudioItem>()
			{
				new() { Name = "valid_sound_name", Clip = AudioClip.Create("valid_sound_name", 1, 1, 1000, true) },
				new() { Name = "valid_music_name", Clip = AudioClip.Create("valid_music_name", 1, 1, 1000, true) },
				new() { Name = "valid_clip_name", Clip = AudioClip.Create("valid_clip_name", 1, 1, 1000, true) }
			};
			var options = new AudioService.Options()
			{
				AudioPlayerFactory = () => new GameObject().AddComponent<FakeAudioPlayer>(),
				AudioBanks = new[] { audioBank }
			};
			_audioService = new AudioService(localStorage, pool, assetLoader, options);
			_audioService.Initialize();
		}

		[Test]
		public void PlaySound_WithValidName_ReturnsTrue()
		{
			// Arrange
			string soundName = "valid_sound_name";

			// Act
			bool result = _audioService.PlaySound(soundName);

			// Assert
			Assert.IsTrue(result);
		}

		[Test]
		public void PlaySound_WithInvalidName_ReturnsFalse()
		{
			// Arrange
			string soundName = "invalid_sound_name";

			// Act
			bool result = _audioService.PlaySound(soundName);

			// Assert
			Assert.IsFalse(result);
		}

		[Test]
		public void PlayMusic_WithValidName_ReturnsTrue()
		{
			// Arrange
			string musicName = "valid_music_name";

			// Act
			bool result = _audioService.PlayMusic(musicName);

			// Assert
			Assert.IsTrue(result);
		}

		[Test]
		public void PlayMusic_WithInvalidName_ReturnsFalse()
		{
			// Arrange
			string musicName = "invalid_music_name";

			// Act
			bool result = _audioService.PlayMusic(musicName);

			// Assert
			Assert.IsFalse(result);
		}
	}

	public class FakeAudioPlayer : AudioPlayer
	{
		override public AudioItem PlayingItem { get; protected set; }

		override public bool PlayMusic(AudioItem item, MusicOptions options)
		{
			if (item == null || !item.IsValid) return false;
			PlayingItem = item;
			return true;
		}

		override public bool PlaySound(AudioItem item, SoundFX sound)
		{
			if (item == null || !item.IsValid) return false;
			PlayingItem = item;
			return true;
		}

		override public void Stop(float fadeDuration)
		{
			PlayingItem = null;
		}
	}

	public class FakeGameObjectService : IGameObjectService
	{
		public GameObject CreateEmptyObject(string name = null) => new(name);

		public T CreateObject<T>() where T : Component => new GameObject().AddComponent<T>();

		public void Destroy(GameObject obj, float delay = 0)
		{
			// Do nothing
		}

		public void DontDestroyOnLoad(GameObject obj)
		{
			// Do nothing
		}

		public T Find<T>(bool includeInactive = false) where T : Component
		{
			// Do nothing
			return default;
		}

		public T[] FindAll<T>(bool includeInactive = false) where T : Component
		{
			// Do nothing
			return new T[0];
		}

		public GameObject Instantiate(GameObject prefab) => Object.Instantiate(prefab);

		public T Instantiate<T>(T prefab) where T : Component => Object.Instantiate(prefab);

		public T Instantiate<T>(GameObject prefab) where T : Component => Object.Instantiate(prefab).GetComponent<T>();

		public UniTask<GameObject> InstantiateAsync(GameObject prefab)
		{
			throw new System.NotImplementedException();
		}

		public UniTask<T> InstantiateAsync<T>(T prefab) where T : Component
		{
			throw new System.NotImplementedException();
		}

		public UniTask<T> InstantiateAsync<T>(GameObject prefab) where T : Component
		{
			throw new System.NotImplementedException();
		}
	}

	public class FakeLocalStorageService : ILocalStorageService
	{
		private readonly Dictionary<string, object> _values = new();

		public T ReadValue<T>(string key, T defaultValue = default) => _values.TryGetValue(key, out var value) ? (T)value : defaultValue;

		public void WriteValue<T>(string key, T value = default) => _values[key] = value;
	}

	public class FakePoolService : IGameObjectPoolService
	{
		public IGameObjectPool GetOrCreatePool(GameObject prefab)
		{
			throw new System.NotImplementedException();
		}

		public IGameObjectPool GetOrCreatePool<T>() where T : Component
		{
			throw new System.NotImplementedException();
		}

		public IGameObjectPool SetupPool(GameObject prefab, PoolOptions options = default)
		{
			throw new System.NotImplementedException();
		}

		public IGameObjectPool SetupPool<T>(PoolOptions options = default) where T : Component
		{
			throw new System.NotImplementedException();
		}
	}

	public class FakeAssetLoaderService : IAssetLoaderService
	{
		private readonly List<AssetData> _assets = new();

		public void AddFakeAsset<T>(string path, T data) where T : Object
		{
			_assets.Add(new AssetData { path = path, asset = data });
		}

		public T LoadAssetFromResources<T>(string path) where T : Object
		{
			return (T)_assets.Find(a => a.path == path).asset;
		}

		public UniTask<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object
		{
			return UniTask.FromResult(LoadAssetFromResources<T>(path));
		}

		public T[] LoadAssetsFromResources<T>(string path) where T : Object
		{
			return _assets.FindAll(a => a.path == path)
				.Select(a => (T)a.asset)
				.ToArray();
		}

		private struct AssetData
		{
			public string path;
			public Object asset;
		}
	}
}