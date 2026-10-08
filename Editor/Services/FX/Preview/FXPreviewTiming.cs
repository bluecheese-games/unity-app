using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Preview-only playback policy. Measuring an effect is FXParticleGraph's job and is shared with the
	/// runtime; what is left here is how the preview chooses to bound what it measured.
	/// </summary>
	public static class FXPreviewTiming
	{
		/// <summary>
		/// Upper bound on any computed preview length. A prefab authored with an absurd duration must not be
		/// able to pin the preview running for minutes.
		/// </summary>
		public const float HardCeiling = 30f;

		private const float _minLength = 0.01f;

		/// <summary>
		/// Length of a single pass of the effect, which is what the time scrubber spans. An explicit
		/// FXDef.Duration wins, so the preview agrees with the rule FXInstance.UpdateFX applies at runtime.
		/// </summary>
		public static float ResolveScrubLength(float referenceLength, bool overrideDuration, float duration)
		{
			if (overrideDuration)
			{
				return Mathf.Max(_minLength, duration);
			}

			return Mathf.Clamp(referenceLength, _minLength, HardCeiling);
		}
	}
}
