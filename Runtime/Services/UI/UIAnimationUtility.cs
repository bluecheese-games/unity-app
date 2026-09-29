using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// Shared per-frame progress driver for <see cref="UIButton"/>'s press animation and
	/// <see cref="Popup"/>'s show/hide transitions -- both are "evaluate progress in [0,1] over a duration,
	/// apply it to whichever channels are active" loops that only differ in which channels they animate, so
	/// the timing/cancellation plumbing lives here once instead of being duplicated in each caller.
	/// </summary>
	internal static class UIAnimationUtility
	{
		/// <summary>
		/// Runs <paramref name="applyProgress"/> once per frame (using unscaled time, so animations still
		/// play while paused) with progress climbing from 0 to 1 over <paramref name="duration"/> seconds,
		/// then once more with exactly 1 to guarantee the final state is applied precisely. A non-positive
		/// duration applies progress 1 immediately and returns. Propagates <see cref="OperationCanceledException"/>
		/// if <paramref name="token"/> is cancelled mid-run; callers that need to leave the animated state
		/// as-is on cancellation (rather than snapping to the end) should catch it themselves.
		/// </summary>
		public static async UniTask RunAsync(float duration, CancellationToken token, Action<float> applyProgress)
		{
			if (duration <= 0f)
			{
				applyProgress(1f);
				return;
			}

			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				applyProgress(Mathf.Clamp01(elapsed / duration));
				await UniTask.Yield(PlayerLoopTiming.Update, token);
			}
			applyProgress(1f);
		}
	}
}
