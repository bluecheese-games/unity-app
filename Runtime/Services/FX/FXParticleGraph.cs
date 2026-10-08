using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// A prefab's ParticleSystems, classified and measured once so that every caller agrees on which of them
	/// have to be driven and on how long the effect lasts. Play/Simulate(withChildren: true) only covers a
	/// system's own subtree, so a prefab with sibling systems -- or with no system on its root at all --
	/// needs each root driven separately.
	/// </summary>
	public sealed class FXParticleGraph
	{
		private static readonly ParticleSystem[] _empty = new ParticleSystem[0];

		private FXParticleGraph(ParticleSystem[] all, ParticleSystem[] roots, bool anyLooping, float loopLength, float effectLength)
		{
			All = all;
			Roots = roots;
			AnyLooping = anyLooping;
			LoopLength = loopLength;
			EffectLength = effectLength;
		}

		/// <summary>Every system that can actually play, in hierarchy order.</summary>
		public ParticleSystem[] All { get; }

		/// <summary>
		/// Systems with no ParticleSystem among their ancestors: the ones a caller has to drive itself.
		/// Everything else is reached through withChildren.
		/// </summary>
		public ParticleSystem[] Roots { get; }

		public bool IsEmpty => All.Length == 0;

		/// <summary>At least one system repeats instead of ending on its own.</summary>
		public bool AnyLooping { get; }

		/// <summary>Longest single loop among the looping systems, start delay included.</summary>
		public float LoopLength { get; }

		/// <summary>
		/// Time until the last particle of the whole effect has died, sub-emitter chains included.
		/// Meaningless for a looping prefab, which by definition never reaches it.
		/// </summary>
		public float EffectLength { get; }

		/// <summary>
		/// The one length worth quoting for a prefab: a single loop if it loops, the full effect otherwise.
		/// Both FXDef.Duration and the inspector preview's scrubber derive from this, so they cannot disagree.
		/// </summary>
		public float ReferenceLength => AnyLooping ? LoopLength : EffectLength;

		public static FXParticleGraph Build(GameObject root)
		{
			if (root == null)
			{
				return new FXParticleGraph(_empty, _empty, false, 0f, 0f);
			}

			var candidates = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			var all = new List<ParticleSystem>(candidates.Length);
			var roots = new List<ParticleSystem>(candidates.Length);

			foreach (var system in candidates)
			{
				if (!IsEnabledUnder(system.transform, root.transform))
				{
					continue;
				}

				all.Add(system);

				if (!HasParticleSystemAncestor(system.transform, root.transform))
				{
					roots.Add(system);
				}
			}

			Measure(all, out bool anyLooping, out float loopLength, out float effectLength);

			return new FXParticleGraph(all.ToArray(), roots.ToArray(), anyLooping, loopLength, effectLength);
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

		/// <summary>
		/// Last moment a system actually emits anything. This is NOT main.duration: duration is the window
		/// during which the system is allowed to emit, while a burst-only system fires everything at its
		/// burst time and never emits again. Measuring from duration would leave an effect "running" on an
		/// empty frame for however long the system had left to emit nothing.
		/// </summary>
		public static float LastEmissionTime(ParticleSystem system)
		{
			var main = system.main;
			var emission = system.emission;

			if (!emission.enabled)
			{
				return 0f;
			}

			// Rate-based emission keeps producing particles right up to the end of the window.
			if (MaxOf(emission.rateOverTime) > 0f || MaxOf(emission.rateOverDistance) > 0f)
			{
				return main.duration;
			}

			float last = 0f;
			for (int i = 0; i < emission.burstCount; i++)
			{
				var burst = emission.GetBurst(i);

				// A non-positive cycle count means "repeat forever", so the burst runs for the whole window.
				float burstEnd = burst.cycleCount <= 0
					? main.duration
					: burst.time + ((burst.cycleCount - 1) * burst.repeatInterval);

				last = Mathf.Max(last, Mathf.Min(burstEnd, main.duration));
			}

			return last;
		}

		private static void Measure(List<ParticleSystem> systems, out bool anyLooping, out float loopLength, out float effectLength)
		{
			anyLooping = false;
			loopLength = 0f;
			effectLength = 0f;

			var subEmitterTargets = CollectSubEmitterTargets(systems);
			var visiting = new HashSet<ParticleSystem>();
			bool measuredAny = false;

			foreach (var system in systems)
			{
				var main = system.main;
				if (main.loop)
				{
					anyLooping = true;
					loopLength = Mathf.Max(loopLength, MaxOf(main.startDelay) + main.duration);
				}

				// A sub-emitter does not start on its own clock, so measuring it from t=0 would under-report
				// the effect. Its contribution is picked up through whichever system triggers it.
				if (subEmitterTargets.Contains(system))
				{
					continue;
				}

				effectLength = Mathf.Max(effectLength, EndTime(system, visiting));
				measuredAny = true;
			}

			if (measuredAny)
			{
				return;
			}

			// Every system is someone's sub-emitter, so none of them was measured and the effect would be
			// reported as instantaneous. Only a reference cycle can produce this, and a zero duration is far
			// more damaging than an over-estimate: FXInstance would stop the effect on its first frame.
			foreach (var system in systems)
			{
				effectLength = Mathf.Max(effectLength, EndTime(system, visiting));
			}
		}

		private static HashSet<ParticleSystem> CollectSubEmitterTargets(List<ParticleSystem> systems)
		{
			var targets = new HashSet<ParticleSystem>();

			foreach (var system in systems)
			{
				var sub = system.subEmitters;
				if (!sub.enabled)
				{
					continue;
				}

				for (int i = 0; i < sub.subEmittersCount; i++)
				{
					var target = sub.GetSubEmitterSystem(i);
					if (target != null)
					{
						targets.Add(target);
					}
				}
			}

			return targets;
		}

		/// <summary>
		/// When everything this system is responsible for has died, measured from the moment it starts.
		/// Sub-emitters make this recursive: a burst that spawns a second system on death does not end when
		/// its own particles die, it ends when the spawned ones do.
		/// </summary>
		private static float EndTime(ParticleSystem system, HashSet<ParticleSystem> visiting)
		{
			var main = system.main;
			float emissionEnd = MaxOf(main.startDelay) + LastEmissionTime(system);
			float ownSpan = emissionEnd + MaxOf(main.startLifetime);

			// Nothing stops an author from wiring two systems as each other's sub-emitter. Unity tolerates it;
			// an unguarded recursion here would not.
			if (!visiting.Add(system))
			{
				return ownSpan;
			}

			float end = ownSpan;
			var sub = system.subEmitters;

			if (sub.enabled)
			{
				for (int i = 0; i < sub.subEmittersCount; i++)
				{
					var child = sub.GetSubEmitterSystem(i);
					if (child == null)
					{
						continue;
					}

					// Birth fires while the parent is still emitting; everything else (Death, Collision,
					// Trigger, Manual) can happen as late as the parent's last particle, which is the only
					// bound we can state without simulating.
					float triggeredAt = sub.GetSubEmitterType(i) == ParticleSystemSubEmitterType.Birth
						? emissionEnd
						: ownSpan;

					end = Mathf.Max(end, triggeredAt + EndTime(child, visiting));
				}
			}

			visiting.Remove(system);
			return end;
		}

		/// <summary>
		/// includeInactive is required to find systems at all when the root itself is inactive -- the normal
		/// state of a pooled FXInstance between uses -- but it also picks up branches the author deliberately
		/// switched off. Those never play, so they must not be driven or measured.
		/// The root's own activeSelf is deliberately not tested, for that same pooling reason.
		/// </summary>
		private static bool IsEnabledUnder(Transform node, Transform root)
		{
			for (var current = node; current != null && current != root; current = current.parent)
			{
				if (!current.gameObject.activeSelf)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Bounded at the graph root on purpose: an FX parented under some unrelated ParticleSystem in the
		/// scene must not have its own roots reclassified as children of it.
		/// </summary>
		private static bool HasParticleSystemAncestor(Transform node, Transform root)
		{
			if (node == root)
			{
				return false;
			}

			for (var current = node.parent; current != null; current = current.parent)
			{
				if (current.GetComponent<ParticleSystem>() != null)
				{
					return true;
				}

				if (current == root)
				{
					return false;
				}
			}

			return false;
		}
	}
}
