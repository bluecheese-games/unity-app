using BlueCheese.Core.Utils;
using System;
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

		// Every system that has to be driven explicitly. Play/Stop(withChildren: true) reaches a system's own
		// subtree, so this is only the roots -- but it is all of them, including when the prefab has none on
		// its own root, which used to leave the whole effect silent.
		private ParticleSystem[] _roots = Array.Empty<ParticleSystem>();

		// Which systems the scalers reach. Same as _roots unless the def opts into scaling nested ones.
		private ParticleSystem[] _scaleTargets = Array.Empty<ParticleSystem>();

		// Note: intentionally independent from _isPaused. Pausing must not make the instance
		// look "dead" to FXService, otherwise it gets despawned back to the pool mid-effect
		// (see Pause()/Resume() below).
		public bool IsAlive => _isPlaying && gameObject.activeInHierarchy;

		public void Setup(FXDef fxDef)
		{
			_def = fxDef;

			var graph = FXParticleGraph.Build(gameObject);
			_roots = graph.Roots;
			_scaleTargets = fxDef.ScaleNestedSystems ? graph.All : graph.Roots;
		}

		public void UpdateFX(float deltaTime)
		{
			if (!IsAlive || _isPaused)
			{
				return;
			}

			_timeElapsed += deltaTime;

			// All of them, not the first one: systems with different lifetimes would otherwise end the whole
			// effect as soon as the shortest of them was done.
			if (_roots.Length > 0 && AreAllRootsStopped())
			{
				Stop();
				return;
			}

			// When Duration wasn't explicitly overridden, it's auto-derived from the prefab's
			// ParticleSystems purely as an editor preview reference (see FXDef.AutoDeriveDuration),
			// and may reflect a looping system's main.duration. Only enforce it as a hard runtime
			// stop when the designer explicitly opted in (OverrideDuration) or nothing loops —
			// otherwise a looping FX would be cut short shortly after it starts.
			bool enforceDuration = _def.OverrideDuration || _roots.Length == 0 || !AnyRootLoops();
			if (enforceDuration && _def.Duration > 0f && _timeElapsed >= _def.Duration)
			{
				Stop();
			}
		}

		private bool AreAllRootsStopped()
		{
			foreach (var root in _roots)
			{
				if (!root.isStopped)
				{
					return false;
				}
			}

			return true;
		}

		private bool AnyRootLoops()
		{
			foreach (var root in _roots)
			{
				if (root.main.loop)
				{
					return true;
				}
			}

			return false;
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
				foreach (var target in _scaleTargets)
				{
					scaler.Apply(target, _scaleValue);
				}
			}

			foreach (var root in _roots)
			{
				root.Play(withChildren: true);
			}
		}

		public void StopEmitting()
		{
			if (!_isPlaying)
			{
				return;
			}

			if (_roots.Length == 0)
			{
				Stop();
				return;
			}

			// Stop the particle systems from emitting new particles
			// Once all existing particles have died, they will stop on their own
			foreach (var root in _roots)
			{
				root.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

			foreach (var root in _roots)
			{
				root.Pause(withChildren: true);
			}
		}

		public void Resume()
		{
			if (!_isPlaying || !_isPaused)
			{
				return;
			}

			_isPaused = false;

			foreach (var root in _roots)
			{
				root.Play(withChildren: true);
			}
		}

		private void OnDestroy() => Stop();
	}
}
