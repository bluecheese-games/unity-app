//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXInstance : MonoBehaviour
	{
		private float _duration;
		private Transform _target;
		private Vector3 _offset;
		private bool _isPlaying;
		private float _timeElapsed;
		private ParticleSystem _particleSystem;

		public bool IsAlive => _isPlaying && gameObject.activeInHierarchy;

		public void Setup(FXDef fxDef)
		{
			_duration = fxDef.Duration;
			_particleSystem = GetComponent<ParticleSystem>();
		}

		public void UpdateFX(float deltaTime)
		{
			if (!IsAlive)
			{
				return;
			}

			_timeElapsed += deltaTime;

			if (_particleSystem != null && _particleSystem.isStopped)
			{
				Stop();
			}
			else if (_duration > 0f && _timeElapsed >= _duration)
			{
				Stop();
			}

			if (_target)
			{
				transform.SetPositionAndRotation(_target.position + _offset, _target.rotation);
			}
		}

		public void Play(Transform target, Vector3 offset = default)
		{
			_target = target;
			_offset = offset;
			Play(target.position + _offset);
		}

		public void Play(Vector3 position)
		{
			if (_isPlaying)
			{
				return;
			}

			_isPlaying = true;
			_timeElapsed = 0f;
			if (_target != null)
			{
				var fxTarget = _target.gameObject.AddOrGetComponent<FXTarget>();
				fxTarget.Subscribe(OnTargetDestroyed);
			}

			transform.position = position;

			Services.Get<IFXService>().RegisterInstance(this);
		}

		private void OnTargetDestroyed()
		{
			_target = null;
			Stop();
		}

		public void Stop()
		{
			if (!_isPlaying)
			{
				return;
			}

			_isPlaying = false;
			gameObject.SetActive(false);

			if (_target != null && _target.TryGetComponent<FXTarget>(out var fxTarget))
			{
				fxTarget.Unsubscribe(OnTargetDestroyed);
			}
		}

		public void Pause() => _isPlaying = false;

		public void Resume() => _isPlaying = true;

		private void OnDestroy()
		{
			if (_target != null && _target.TryGetComponent<FXTarget>(out var fxTarget))
			{
				fxTarget.Unsubscribe(OnTargetDestroyed);
			}
		}
	}
}
