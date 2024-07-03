//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections;
using UnityEngine;

namespace BlueCheese.App
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceHandle : MonoBehaviour, IRecyclable
    {
        private IAudioService _audioService;
        private IPoolService _poolService;

        private AudioSource _audioSource;
        private Transform _target;
        private Vector3 _offset;

        public string Key { get; private set; }
        public bool IsInitialized { get; private set; } = false;
        public bool IsPlaying { get; private set; } = false;

        public void Initialize(string name, IAudioService audioService, IPoolService poolService)
        {
            Key = name;
            _audioService = audioService;
            _poolService = poolService;
            IsInitialized = true;
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void LateUpdate()
        {
            if (!IsPlaying || _audioSource == null)
            {
                return;
            }

            if (_target != null)
            {
                transform.position = _target.position + _offset;
            }

            if (!_audioSource.isPlaying)
            {
                IsPlaying = false;
                _poolService.Despawn(gameObject);
            }
        }

        public void PlaySound(AudioClip clip, SoundOptions options)
        {
            if (!Application.isPlaying || _audioSource == null)
            {
                return;
            }

            _audioSource.clip = clip;
            _audioSource.volume = options.Volume * _audioService.MasterSoundVolume;
            _audioSource.loop = options.Loop;
            _audioSource.pitch = options.Pitch;
            _audioSource.spatialize = options.Spacial.IsSpacialized;
            if (options.Spacial.IsSpacialized)
            {
                _audioSource.minDistance = options.Spacial.MinDistance;
                _audioSource.maxDistance = options.Spacial.MaxDistance;
                _audioSource.rolloffMode = options.Spacial.RolloffMode;
                _target = options.Spacial.Target;
                if (_target != null)
                {
                    _offset = options.Spacial.Position;
                }
                else
                {
                    transform.position = options.Spacial.Position;
                }
            }

            _audioSource.PlayDelayed(options.Delay);
            IsPlaying = true;
        }

        public void PlayMusic(AudioClip clip, MusicOptions options)
        {
            if (!Application.isPlaying || _audioSource == null)
            {
                return;
            }

            _audioSource.clip = clip;
            _audioSource.volume = options.Volume * _audioService.MasterSoundVolume;
            _audioSource.loop = true;
            _audioSource.spatialize = false;
            _audioSource.Play();
            IsPlaying = true;
        }

        public void Stop(float fadeDuration)
        {
            if (!Application.isPlaying || !IsPlaying || _audioSource == null)
            {
                return;
            }

            if (fadeDuration > 0 && gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeStopRoutine(fadeDuration));
            }
            else
            {
                Stop();
            }
        }

        private void Stop()
        {
            IsPlaying = false;
            _audioSource.Stop();
            _target = null;
        }

        private IEnumerator FadeStopRoutine(float fadeDuration)
        {
            float fadeTimeleft = fadeDuration;
            float startVolume = _audioSource.volume;
            while (fadeTimeleft > 0)
            {
                yield return null;
                fadeTimeleft -= Time.deltaTime;
                _audioSource.volume = (fadeTimeleft / fadeDuration) * startVolume;
            }
            Stop();
        }

        void IRecyclable.Recycle()
        {
            Stop();
        }

        private void OnDestroy()
        {
            _audioSource = null;
        }
    }
}