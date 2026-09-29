using System;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// A single animated channel: a target value plus the curve used to animate towards/away from it.
	/// Shared shape for every "Custom" animation channel (<see cref="UIButton"/>'s Custom press animation,
	/// <see cref="Popup"/>'s Custom show/hide transition) so each one only needs an
	/// <c>Optional&lt;CurveParam&gt;</c> field instead of a bespoke value+curve pair.
	/// </summary>
	[Serializable]
	public class CurveParam
	{
		public float Value;
		public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
	}
}
