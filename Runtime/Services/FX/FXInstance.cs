//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXInstance : MonoBehaviour, IRecyclable
	{
		private FXDef _def;
		private Transform _target;
		private Vector3 _offsetPosition;
		private Vector3 _offsetRotation;
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
		}

		public void Scale(float value) => _scaleValue = value;

		public void PlayOnTarget(Transform target, Vector3 offsetPosition = default, Vector3 offsetRotation = default)
		{
			if (_isPlaying)
			{
				return;
			}

			_target = target;
			_offsetPosition = offsetPosition;
			_offsetRotation = offsetRotation;
			PlayAt(target.position + _offsetPosition, target.rotation * Quaternion.Euler(_offsetRotation));
		}

		public void PlayAt(Vector3 position, Quaternion rotation = default)
		{
			if (_isPlaying)
			{
				return;
			}

			transform.SetPositionAndRotation(position, rotation);

			Play();
		}

		private void Play()
		{
			_isPlaying = true;
			_timeElapsed = 0f;
			if (_target != null)
			{
				FXTarget.Register(_target.gameObject, this, _offsetPosition, _offsetRotation);
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

		public void StopEmitting()
		{
			if (!_isPlaying)
			{
				return;
			}

			if (_particleSystem != null)
			{
				// Stop the particle system from emitting new particles
				// Once all existing particles have died, the particle system will stop
				_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			}
			else
			{
				Stop();
			}
		}

		public void Stop()
		{
			if (!_isPlaying)
			{
				return;
			}

			_isPlaying = false;
			gameObject.SetActive(false);

			if (_target != null)
			{
				FXTarget.Unregister(_target.gameObject, this);
				_target = null;
			}
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
