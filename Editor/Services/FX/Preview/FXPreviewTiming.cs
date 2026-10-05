using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Pure timing helpers for the FXDef inspector preview: which ParticleSystems have to be driven,
	/// how long a single pass of the effect lasts, and when playback should stop on its own.
	/// Kept free of editor state so it can be unit tested.
	/// </summary>
	public static class FXPreviewTiming
	{
		/// <summary>
		/// Upper bound on any computed preview length. A prefab authored with an absurd duration (or a
		/// long loop multiplied by MaxLoops) must not be able to pin the preview running for minutes.
		/// </summary>
		public const float HardCeiling = 30f;

		private const float _minLength = 0.01f;

		public struct Measurements
		{
			public bool AnyLooping;

			/// <summary>Longest single loop among the looping systems, start delay included.</summary>
			public float LoopLength;

			/// <summary>Time until the last particle of the last system has died.</summary>
			public float EffectLength;

			/// <summary>Largest start delay in the hierarchy. Used to grace-gate the "nothing is alive" check.</summary>
			public float MaxStartDelay;
		}

		/// <summary>
		/// Returns the systems that have no ParticleSystem among their ancestors. Simulate(withChildren: true)
		/// only covers a system's own subtree, so each of these roots has to be driven separately — a prefab
		/// with sibling systems, or with no system on its root at all, would otherwise be partly frozen.
		/// </summary>
		public static ParticleSystem[] CollectRoots(IReadOnlyList<ParticleSystem> systems)
		{
			var roots = new List<ParticleSystem>(systems.Count);
			foreach (var system in systems)
			{
				var parent = system.transform.parent;
				if (parent != null && parent.GetComponentInParent<ParticleSystem>(includeInactive: true) != null)
				{
					continue;
				}

				roots.Add(system);
			}

			return roots.ToArray();
		}

		/// <summary>
		/// Largest value a MinMaxCurve can yield. Curve modes report the multiplier rather than walking the
		/// keyframes: curves are normalized to [0..1] by convention, so the multiplier is the practical max.
		/// </summary>
		public static float MaxOf(ParticleSystem.MinMaxCurve curve) => curve.mode switch
		{
			ParticleSystemCurveMode.Constant => curve.constant,
			ParticleSystemCurveMode.TwoConstants => curve.constantMax,
			_ => curve.curveMultiplier,
		};

		public static Measurements Measure(IReadOnlyList<ParticleSystem> systems)
		{
			var measurements = new Measurements();

			foreach (var system in systems)
			{
				var main = system.main;
				float delay = MaxOf(main.startDelay);
				float lifetime = MaxOf(main.startLifetime);

				measurements.MaxStartDelay = Mathf.Max(measurements.MaxStartDelay, delay);
				measurements.EffectLength = Mathf.Max(measurements.EffectLength, delay + main.duration + lifetime);

				if (main.loop)
				{
					measurements.AnyLooping = true;
					measurements.LoopLength = Mathf.Max(measurements.LoopLength, delay + main.duration);
				}
			}

			return measurements;
		}

		/// <summary>
		/// Length of a single pass of the effect: one loop for a looping prefab, the full lifetime otherwise.
		/// This is what the time scrubber spans. An explicit FXDef.Duration wins, so the preview agrees with
		/// the rule FXInstance.UpdateFX applies at runtime.
		/// </summary>
		public static float ResolveScrubLength(Measurements measurements, bool overrideDuration, float duration)
		{
			if (overrideDuration)
			{
				return Mathf.Max(_minLength, duration);
			}

			float length = measurements.AnyLooping ? measurements.LoopLength : measurements.EffectLength;
			return Mathf.Clamp(length, _minLength, HardCeiling);
		}
	}
}
