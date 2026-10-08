using BlueCheese.App.Editor;
using NUnit.Framework;

namespace BlueCheese.Tests.Services
{
	public class Tests_FXPreviewTiming
	{
		[Test]
		public void Test_ResolveScrubLength_NoOverride_SpansTheReferenceLength()
		{
			float length = FXPreviewTiming.ResolveScrubLength(referenceLength: 2f, overrideDuration: false, duration: 99f);

			Assert.That(length, Is.EqualTo(2f));
		}

		[Test]
		public void Test_ResolveScrubLength_OverrideDuration_WinsOverTheMeasurement()
		{
			float length = FXPreviewTiming.ResolveScrubLength(referenceLength: 7f, overrideDuration: true, duration: 1.25f);

			Assert.That(length, Is.EqualTo(1.25f));
		}

		[Test]
		public void Test_ResolveScrubLength_AbsurdReferenceLength_ClampedToCeiling()
		{
			float length = FXPreviewTiming.ResolveScrubLength(referenceLength: 1000f, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.EqualTo(FXPreviewTiming.HardCeiling));
		}

		[Test]
		public void Test_ResolveScrubLength_NothingMeasured_ReturnsNonZero()
		{
			float length = FXPreviewTiming.ResolveScrubLength(referenceLength: 0f, overrideDuration: false, duration: 0f);

			Assert.That(length, Is.GreaterThan(0f), "A zero-length slider cannot be dragged.");
		}

		[Test]
		public void Test_ResolveScrubLength_OverrideWithZeroDuration_StillReturnsNonZero()
		{
			float length = FXPreviewTiming.ResolveScrubLength(referenceLength: 3f, overrideDuration: true, duration: 0f);

			Assert.That(length, Is.GreaterThan(0f));
		}
	}
}
