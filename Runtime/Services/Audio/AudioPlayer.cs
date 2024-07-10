//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Collections;
using UnityEngine;

namespace BlueCheese.App
{
	[RequireComponent(typeof(AudioSource))]
	public class AudioPlayer : MonoBehaviour, IRecyclable, IAudioPlayer
	{
		[Injectable] private IAudioService _audio;
		[Injectable] private IPoolService _pool;

		private AudioSource _audioSource;
		private Transform _target;
		private Vector3 _offset;

		public AudioItem PlayingItem { get; private set; }
		public bool IsPlaying => PlayingItem != null;

		private void Awake()
		{
			Services.Inject(this);
			_audioSource = GetComponent<AudioSource>();
			DontDestroyOnLoad(gameObject);
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
				Stop();
			}
		}

		public bool PlaySound(AudioItem item, SoundOptions options)
		{
			if (!Application.isPlaying || _audioSource == null || !item.IsValid)
			{
				return false;
			}

			_audioSource.clip = item.Clip;
			_audioSource.volume = item.Volume * options.Volume * _audio.MasterSoundVolume;
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
			PlayingItem = item;

			return true;
		}

		public bool PlayMusic(AudioItem item, MusicOptions options)
		{
			if (!Application.isPlaying || _audioSource == null || !item.IsValid)
			{
				return false;
			}

			_audioSource.clip = item.Clip;
			_audioSource.volume = item.Volume * options.Volume * _audio.MasterSoundVolume;
			_audioSource.loop = true;
			_audioSource.spatialize = false;
			_audioSource.Play();
			PlayingItem = item;

			return true;
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

		private void Stop(bool despawn = true)
		{
			PlayingItem = null;
			_audioSource.Stop();
			_target = null;
			if (despawn)
			{
				_pool.Despawn(gameObject);
			}
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
			Stop(false);
		}

		private void OnDestroy()
		{
			_audioSource = null;
			_target = null;
		}
	}
}