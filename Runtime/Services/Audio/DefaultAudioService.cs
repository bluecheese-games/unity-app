//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
    public class DefaultAudioService : IAudioService
    {
        private const string _masterSoundVolumePrefKey = "master_sound_volume";
        private const string _masterMusicVolumePrefKey = "master_music_volume";

        private readonly ILocalStorageService _localStorageService;
        private readonly IPoolService _poolService;
        private readonly IAssetService _assetService;

        private string _currentMusic = null;
        private string _whenReadyMusicName;
        private MusicOptions _whenReadyMusicOptions;

        private readonly List<AudioSourceHandle> _audioSources = new();
        private readonly Dictionary<string, AudioClip> _audioClips = new();

        public DefaultAudioService(ILocalStorageService localStorageService, IPoolService poolService, IAssetService assetService)
        {
            _localStorageService = localStorageService;
            _poolService = poolService;
            _assetService = assetService;
        }

        public void Initialize()
        {
            if (!IsReady)
            {
                IsReady = true;

                InitializePool();
                LoadAudioBanks();

                // Starts playing music if request has been made
                if (_whenReadyMusicName != null)
                {
                    PlayMusic(_whenReadyMusicName, _whenReadyMusicOptions);
                }
            }
        }

        private void InitializePool()
        {
            _poolService.Initialize<AudioSourceHandle>(new PoolOptions()
            {
                InitialCapacity = 10,
                UseContainer = true,
                DontDestroyOnLoad = true
            });
        }

        private void LoadAudioBanks()
        {
            var banks = _assetService.LoadAssetsFromResources<AudioBank>("Audio");
            foreach (var bank in banks)
            {
                foreach (var item in bank.Items)
                {
                    _audioClips.Add(item.Name, item.Clip);
                }
            }
        }

        public bool IsReady { get; private set; } = false;

        public float MasterSoundVolume
        {
            get
            {
                return _localStorageService.ReadValue(_masterSoundVolumePrefKey, 1f);
            }
            set
            {
                _localStorageService.WriteValue(_masterSoundVolumePrefKey, value);
            }
        }

        public float MasterMusicVolume
        {
            get
            {
                return _localStorageService.ReadValue(_masterMusicVolumePrefKey, 1f);
            }
            set
            {
                _localStorageService.WriteValue(_masterMusicVolumePrefKey, value);
            }
        }

        public void PlaySound(string name) => PlaySound(name, SoundOptions.Default);

        public void PlaySound(string name, SoundOptions options)
        {
            if (!IsReady || name == null)
            {
                return;
            }

            var clip = GetAudioClip(name);
            if (clip != null)
            {
                var audioSource = GetAvailableAudioSource(name);
                audioSource.PlaySound(clip, options);
            }
        }

        public void StopSound(string name, float fadeDuration = 0f)
        {
            if (!IsReady || name == null)
            {
                return;
            }

            StopPlayingAudioSources(name, fadeDuration);
        }

        public void PlayMusic(string name) => PlayMusic(name, MusicOptions.Default);

        public void PlayMusic(string name, MusicOptions options)
        {
            if (!IsReady)
            {
                // Keep it for when service is ready
                _whenReadyMusicName = name;
                _whenReadyMusicOptions = options;
                return;
            }

            if (name == null || name == _currentMusic)
            {
                return;
            }

            StopMusic(_currentMusic, options.FadeDurationSec);

            var clip = GetAudioClip(name);
            if (clip != null)
            {
                var audioSource = GetAvailableAudioSource(name);
                audioSource.PlayMusic(clip, options);
                _currentMusic = name;
            }
        }

        public void StopMusic(string name, float fadeDuration = 0f)
        {
            if (!IsReady || name == null || _currentMusic == null)
            {
                return;
            }

            StopPlayingAudioSources(name, fadeDuration);

            _currentMusic = null;
        }

        private void StopPlayingAudioSources(string key, float fadeDuration)
        {
            foreach (var audioSource in _audioSources)
            {
                if (audioSource.IsPlaying && audioSource.Key == key)
                {
                    audioSource.Stop(fadeDuration);
                }
            }
        }

        private AudioClip GetAudioClip(string name)
        {
            if (_audioClips.TryGetValue(name, out var clip))
            {
                return clip;
            }
            return null;
        }

        private AudioSourceHandle GetAvailableAudioSource(string key)
        {
            var audioSource = _poolService.Spawn<AudioSourceHandle>();
            if (!audioSource.IsInitialized)
            {
                audioSource.Initialize(key, this, _poolService);
                _audioSources.Add(audioSource);
            }

            return audioSource;
        }
    }
}