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
