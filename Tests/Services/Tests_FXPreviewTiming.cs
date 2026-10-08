using BlueCheese.App.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	public class Tests_FXPreviewTiming
	{
		[Test]
		public void Test_MaxOf_Constant_ReturnsConstant()
		{
			var curve = new ParticleSystem.MinMaxCurve(2.5f);

			float max = FXPreviewTiming.MaxOf(curve);

			Assert.That(max, Is.EqualTo(2.5f));
		}

		[Test]
		public void Test_MaxOf_TwoConstants_ReturnsUpperBound()
		{
			var curve = new ParticleSystem.MinMaxCurve(1f, 4f);

			float max = FXPreviewTiming.MaxOf(curve);

			Assert.That(max, Is.EqualTo(4f));
		}

		[Test]
		public void Test_MaxOf_Curve_ReturnsMultiplier()
		{
			var curve = new ParticleSystem.MinMaxCurve(3f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

			float max = FXPreviewTiming.MaxOf(curve);

			Assert.That(max, Is.EqualTo(3f));
		}

		[Test]
		public void Test_ResolveScrubLength_Looping_SpansOneLoop()
		{
			var measurements = Looping(loopLength: 2f, effectLength: 7f);

			float length = FXPreviewTiming.ResolveScrubLength(measurements, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.EqualTo(2f));
		}

		[Test]
		public void Test_ResolveScrubLength_NotLooping_SpansWholeEffect()
		{
			var measurements = OneShot(effectLength: 7f);

			float length = FXPreviewTiming.ResolveScrubLength(measurements, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.EqualTo(7f));
		}

		[Test]
		public void Test_ResolveScrubLength_OverrideDuration_WinsOverMeasurements()
		{
			var measurements = Looping(loopLength: 2f, effectLength: 7f);

			float length = FXPreviewTiming.ResolveScrubLength(measurements, overrideDuration: true, duration: 1.25f);

			Assert.That(length, Is.EqualTo(1.25f));
		}

		[Test]
		public void Test_ResolveScrubLength_AbsurdEffectLength_ClampedToCeiling()
		{
			var measurements = OneShot(effectLength: 1000f);

			float length = FXPreviewTiming.ResolveScrubLength(measurements, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.EqualTo(FXPreviewTiming.HardCeiling));
		}

		[Test]
		public void Test_ResolveScrubLength_EmptyMeasurements_ReturnsNonZero()
		{
			var measurements = default(FXPreviewTiming.Measurements);

			float length = FXPreviewTiming.ResolveScrubLength(measurements, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.GreaterThan(0f));
		}

		[Test]
		public void Test_LastEmissionTime_BurstAtZero_IsZeroNotDuration()
		{
			var system = MakeSystem(duration: 1f, lifetime: 1f, burstTime: 0f, burstCycles: 1, rateOverTime: 0f);

			float last = FXPreviewTiming.LastEmissionTime(system);

			Assert.That(last, Is.EqualTo(0f));
			DestroySystem(system);
		}

		[Test]
		public void Test_LastEmissionTime_ContinuousRate_IsDuration()
		{
			var system = MakeSystem(duration: 1f, lifetime: 1f, burstTime: 0f, burstCycles: 0, rateOverTime: 10f);

			float last = FXPreviewTiming.LastEmissionTime(system);

			Assert.That(last, Is.EqualTo(1f));
			DestroySystem(system);
		}

		[Test]
		public void Test_LastEmissionTime_RepeatingBurst_AccountsForCycles()
		{
			var system = MakeSystem(duration: 5f, lifetime: 1f, burstTime: 0.5f, burstCycles: 3, rateOverTime: 0f, repeatInterval: 0.25f);

			float last = FXPreviewTiming.LastEmissionTime(system);

			Assert.That(last, Is.EqualTo(1f).Within(0.0001f)); // 0.5 + 2 * 0.25
			DestroySystem(system);
		}

		[Test]
		public void Test_Measure_BurstOnlySystem_EffectEndsWithTheLastParticle()
		{
			// The regression this guards: duration + lifetime would say 2s for an effect that is over at 1s.
			var system = MakeSystem(duration: 1f, lifetime: 1f, burstTime: 0f, burstCycles: 1, rateOverTime: 0f);

			var measurements = FXPreviewTiming.Measure(new[] { system });

			Assert.That(measurements.EffectLength, Is.EqualTo(1f));
			DestroySystem(system);
		}

		[Test]
		public void Test_Measure_ContinuousSystem_StillAllowsForTheTail()
		{
			var system = MakeSystem(duration: 1f, lifetime: 1f, burstTime: 0f, burstCycles: 0, rateOverTime: 10f);

			var measurements = FXPreviewTiming.Measure(new[] { system });

			Assert.That(measurements.EffectLength, Is.EqualTo(2f));
			DestroySystem(system);
		}

		private static ParticleSystem MakeSystem(float duration, float lifetime, float burstTime, int burstCycles, float rateOverTime, float repeatInterval = 0.01f)
		{
			var go = new GameObject("Tests_FXPreviewTiming") { hideFlags = HideFlags.HideAndDontSave };
			var system = go.AddComponent<ParticleSystem>();

			var main = system.main;
			main.duration = duration;
			main.startLifetime = lifetime;
			main.startDelay = 0f;
			main.loop = false;

			var emission = system.emission;
			emission.enabled = true;
			emission.rateOverTime = rateOverTime;
			emission.rateOverDistance = 0f;
			emission.SetBursts(burstCycles > 0
				? new[] { new ParticleSystem.Burst(burstTime, 30, burstCycles, repeatInterval) }
				: new ParticleSystem.Burst[0]);

			return system;
		}

		private static void DestroySystem(ParticleSystem system) => Object.DestroyImmediate(system.gameObject);

		private static FXPreviewTiming.Measurements Looping(float loopLength, float effectLength) => new()
		{
			AnyLooping = true,
			LoopLength = loopLength,
			EffectLength = effectLength,
		};

		private static FXPreviewTiming.Measurements OneShot(float effectLength) => new()
		{
			AnyLooping = false,
			EffectLength = effectLength,
		};
	}
}
