//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXInstance : MonoBehaviour, IRecyclable
	{
		private FXDef _def;
		private Transform _target;
		private Vector3 _offset;
		private bool _isPlaying;
		private float _timeElapsed;
		private float _scaleValue = 1f;
		private ParticleSystem _particleSystem;

		public bool IsAlive => _isPlaying && gameObject.activeInHierarchy;

		public void Setup(FXDef fxDef)
		{
			_def = fxDef;
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
			else if (_def.Duration > 0f && _timeElapsed >= _def.Duration)
			{
				Stop();
			}

			if (_target)
			{
				transform.SetPositionAndRotation(_target.position + _offset, _target.rotation);
			}
		}

		public void Scale(float value) => _scaleValue = value;

		public void PlayOnTarget(Transform target, Vector3 offset = default)
		{
			if (_isPlaying)
			{
				return;
			}

			_target = target;
			_offset = offset;
			PlayAtPosition(target.position + _offset);
		}

		public void PlayAtPosition(Vector3 position)
		{
			if (_isPlaying)
			{
				return;
			}

			transform.position = position;

			Play();
		}

		private void Play()
		{
			_isPlaying = true;
			_timeElapsed = 0f;
			if (_target != null)
			{
				var fxTarget = _target.gameObject.AddOrGetComponent<FXTarget>();
				fxTarget.Subscribe(OnTargetDestroyed);
			}

			foreach (var scaler in _def.Scalers)
			{
				scaler.Apply(_particleSystem, _scaleValue);
			}

			if (_particleSystem != null)
			{
				_particleSystem.Play(true);
			}
		}

		private void OnTargetDestroyed()
		{
			Stop();
		}

		public void Stop()
		{
			_isPlaying = false;
			gameObject.SetActive(false);

			if (_target != null && _target.TryGetComponent<FXTarget>(out var fxTarget))
			{
				fxTarget.Unsubscribe(OnTargetDestroyed);
			}
			_target = null;
		}

		public void OnRecycle()
		{
			_isPlaying = false;
			_scaleValue = 1f;
			_target = null;
		}

		public void Pause() => _isPlaying = false;

		public void Resume() => _isPlaying = true;

		private void OnDestroy() => Stop();
	}
}
