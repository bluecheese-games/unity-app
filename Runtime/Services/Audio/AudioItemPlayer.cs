//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using BlueCheese.Core.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace BlueCheese.App
{

	[RequireComponent(typeof(AudioSource))]
	public class AudioItemPlayer : MonoBehaviour, IRecyclable
	{
		private AudioSource _audioSource;
		private Transform _target;
		private Vector3 _offset;

		public event Action<AudioItemPlayer> OnSoundFinished;

		virtual public AudioItem PlayingItem { get; protected set; }
		public bool IsPlaying => PlayingItem != null;

		private void Awake()
		{
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

		virtual public bool PlaySound(AudioItem item, SoundFX sound)
		{
			if (_audioSource == null || !item.IsValid)
			{
				return false;
			}

			var audioService = Services.Get<IAudioService>();

			_audioSource.clip = item.Clip;
			_audioSource.volume = item.Volume * sound.Options.Volume * audioService.MasterSoundVolume;
			_audioSource.loop = sound.Options.Loop;
			_audioSource.pitch = GetRandomValue(sound.Options.Pitch);
			_audioSource.spatialize = sound.Options.Spacial.IsSpacialized;
			if (sound.Options.Spacial.IsSpacialized)
			{
				_audioSource.minDistance = sound.Options.Spacial.MinDistance;
				_audioSource.maxDistance = sound.Options.Spacial.MaxDistance;
				_audioSource.rolloffMode = sound.Options.Spacial.RolloffMode;
				_target = sound.Options.Spacial.Target;
				if (_target != null)
				{
					_offset = sound.Position;
				}
				else
				{
					transform.position = sound.Position;
				}
			}

			_audioSource.Stop();
			_audioSource.PlayDelayed(sound.Options.Delay);
			PlayingItem = item;

			return true;
		}

		private static float GetRandomValue(Vector2 range)
		{
			if (range.x == range.y)
			{
				return range.x;
			}

			return Services.Get<IRandomService>().Next(range.x, range.y);
		}

		virtual public bool PlayMusic(AudioItem item, MusicOptions options)
		{
			if (!Application.isPlaying || _audioSource == null || !item.IsValid)
			{
				return false;
			}

			var audioService = Services.Get<IAudioService>();

			_audioSource.clip = item.Clip;
			_audioSource.volume = item.Volume * options.Volume * audioService.MasterSoundVolume;
			_audioSource.loop = true;
			_audioSource.spatialize = false;
			_audioSource.Play();
			PlayingItem = item;

			return true;
		}

		virtual public void Stop(float fadeDuration)
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

		private void Stop(bool raiseEvent = true)
		{
			PlayingItem = null;
			_audioSource.Stop();
			_target = null;
			if (raiseEvent)
			{
				OnSoundFinished?.Invoke(this);
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

		void IRecyclable.OnRecycle()
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