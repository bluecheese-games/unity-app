//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.App
{
	public class AudioService : IAudioService
	{
		private const string _masterSoundVolumePrefKey = "master_sound_volume";
		private const string _masterMusicVolumePrefKey = "master_music_volume";

		private readonly ILocalStorageService _localStorage;
		private readonly IGameObjectPoolService _poolService;
		private readonly IAssetLoaderService _assetLoader;
		private readonly Options _options;

		private string _currentMusic = null;
		private string _whenReadyMusicName;
		private MusicOptions _whenReadyMusicOptions;
		private IGameObjectPool _audioPlayerPool;

		private readonly List<AudioPlayer> _audioPlayers = new();
		private readonly Dictionary<string, AudioItem> _audioItems = new();

		public AudioService(ILocalStorageService localStorage, IGameObjectPoolService pool, IAssetLoaderService assetLoader, Options options)
		{
			_localStorage = localStorage;
			_poolService = pool;
			_assetLoader = assetLoader;
			_options = options;

			_options.AudioPlayerFactory ??= () => GetAvailablePlayer();
		}

		public void Initialize()
		{
			if (!IsReady)
			{
				LoadAudioBanks();
				InitializeAudioPool();

				IsReady = true;

				// Starts playing music if request has been made
				if (_whenReadyMusicName != null)
				{
					PlayMusic(_whenReadyMusicName, _whenReadyMusicOptions);
				}
			}
		}

		private void InitializeAudioPool()
		{
			_audioPlayerPool = _poolService.SetupPool<AudioPlayer>(new PoolOptions()
			{
				InitialCapacity = _options.AudioPoolCapacity,
				DontDestroyOnLoad = true,
				UseContainer = true,
			});
		}

		private void LoadAudioBanks()
		{
			List<AudioBank> banks = new();
			if (_options.AudioBankResourcePath != null)
			{
				banks.AddRange(_assetLoader.LoadAssetsFromResources<AudioBank>(_options.AudioBankResourcePath));
			}
			if (_options.AudioBanks != null)
			{
				banks.AddRange(_options.AudioBanks);
			}
			foreach (var bank in banks)
			{
				foreach (var item in bank.Items)
				{
					_audioItems.Add(item.Name, item);
				}
			}
		}

		public bool IsReady { get; private set; } = false;

		public float MasterSoundVolume
		{
			get
			{
				return _localStorage.ReadValue(_masterSoundVolumePrefKey, 1f);
			}
			set
			{
				_localStorage.WriteValue(_masterSoundVolumePrefKey, value);
			}
		}

		public float MasterMusicVolume
		{
			get
			{
				return _localStorage.ReadValue(_masterMusicVolumePrefKey, 1f);
			}
			set
			{
				_localStorage.WriteValue(_masterMusicVolumePrefKey, value);
			}
		}

		public bool PlaySound(string name) => PlaySound(name, SoundOptions.Default);

		public bool PlaySound(string name, SoundOptions options)
		{
			if (!IsReady || name == null)
			{
				return false;
			}

			var player = _options.AudioPlayerFactory();
			if (player != null)
			{
				var item = GetAudioItem(name);
				return player.PlaySound(item, options);
			}
			return false;
		}

		public void StopSound(string name, float fadeDuration = 0f)
		{
			if (!IsReady || name == null)
			{
				return;
			}

			Stop(name, fadeDuration);
		}

		public void StopAllSounds(float fadeDuration = 0)
		{
			if (!IsReady)
			{
				return;
			}

			StopSoundsWhere(player => true, fadeDuration);
		}

        private void StopSoundsWhere(Func<AudioPlayer, bool> predicate, float fadeDuration = 0)
        {
			var players = _audioPlayers.Where(predicate).ToArray();
            foreach (var player in players)
            {
                player.Stop(fadeDuration);
            }
        }

        public bool PlayMusic(string name) => PlayMusic(name, MusicOptions.Default);

		public bool PlayMusic(string name, MusicOptions options)
		{
			if (!IsReady)
			{
				// Keep it for when service is ready
				_whenReadyMusicName = name;
				_whenReadyMusicOptions = options;
				return false;
			}

			if (name == null || name == _currentMusic)
			{
				return false;
			}

			StopMusic(_currentMusic, options.FadeDurationSec);

			var player = _options.AudioPlayerFactory();
			var item = GetAudioItem(name);
			if (player != null && player.PlayMusic(item, options))
			{
				_currentMusic = name;
				return true;
			}
			return false;
		}

		public void StopMusic(string name, float fadeDuration = 0f)
		{
			if (!IsReady || name == null || _currentMusic == null)
			{
				return;
			}

			Stop(name, fadeDuration);

			_currentMusic = null;
		}

		private void Stop(string name, float fadeDuration)
		{
			StopSoundsWhere(player => player.PlayingItem != null && player.PlayingItem.Name == name, fadeDuration);
		}

		private AudioItem GetAudioItem(string name)
		{
			if (_audioItems.TryGetValue(name, out var clip))
			{
				return clip;
			}
			return null;
		}

		private AudioPlayer GetAvailablePlayer()
		{
			AudioPlayer player = _audioPlayerPool.Spawn<AudioPlayer>();
			if (!_audioPlayers.Contains(player))
			{
				_audioPlayers.Add(player);
				player.OnSoundFinished += HandleSoundFinished;
			}
			return player;
		}

		private void HandleSoundFinished(AudioPlayer audioPlayer)
		{
			_audioPlayers.Remove(audioPlayer);
			_audioPlayerPool.Despawn(audioPlayer);
		}

		public struct Options : IOptions
		{
			/// <summary>
			/// Custom audio player factory.
			/// </summary>
			public Func<AudioPlayer> AudioPlayerFactory;

			/// <summary>
			/// Directly provided audio banks.
			/// </summary>
			public AudioBank[] AudioBanks;

			/// <summary>
			/// The resource path where audio banks are located.
			/// </summary>
			public string AudioBankResourcePath;

			/// <summary>
			/// The AudioPlayer pool size.
			/// </summary>
			public int AudioPoolCapacity;
		}
	}
}