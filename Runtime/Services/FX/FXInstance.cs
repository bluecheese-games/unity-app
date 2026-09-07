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
		private bool _isPaused;
		private float _timeElapsed;
		private float _scaleValue = 1f;
		private ParticleSystem _particleSystem;

		// Note: intentionally independent from _isPaused. Pausing must not make the instance
		// look "dead" to FXService, otherwise it gets despawned back to the pool mid-effect
		// (see Pause()/Resume() below).
		public bool IsAlive => _isPlaying && gameObject.activeInHierarchy;

		public void Setup(FXDef fxDef)
		{
			_def = fxDef;
			_particleSystem = GetComponent<ParticleSystem>();
		}

		public void UpdateFX(float deltaTime)
		{
			if (!IsAlive || _isPaused)
			{
				return;
			}

			_timeElapsed += deltaTime;
			if (_particleSystem != null && _particleSystem.isStopped)
			{
				Stop();
				return;
			}

			// When Duration wasn't explicitly overridden, it's auto-derived from the prefab's
			// ParticleSystems purely as an editor preview reference (see FXDef.AutoDeriveDuration),
			// and may reflect a looping system's main.duration. Only enforce it as a hard runtime
			// stop when the designer explicitly opted in (OverrideDuration) or the system doesn't
			// loop — otherwise a looping FX would be cut short shortly after it starts.
			bool enforceDuration = _def.OverrideDuration || _particleSystem == null || !_particleSystem.main.loop;
			if (enforceDuration && _def.Duration > 0f && _timeElapsed >= _def.Duration)
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
			_isPaused = false;
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
			_isPaused = false;
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
			_isPaused = false;
			_scaleValue = 1f;
			_target = null;
		}

		public void Pause()
		{
			if (!_isPlaying || _isPaused)
			{
				return;
			}

			_isPaused = true;

			if (_particleSystem != null)
			{
				_particleSystem.Pause(true);
			}
		}

		public void Resume()
		{
			if (!_isPlaying || !_isPaused)
			{
				return;
			}

			_isPaused = false;

			if (_particleSystem != null)
			{
				_particleSystem.Play(true);
			}
		}

		private void OnDestroy() => Stop();
	}
}
