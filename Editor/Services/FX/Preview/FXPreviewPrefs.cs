using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Machine-local preferences for the FXDef inspector preview. These are deliberately global rather
	/// than per-asset: they describe how the user likes to work, not anything about a given effect.
	/// </summary>
	public static class FXPreviewPrefs
	{
		private const string _autoPlayKey = "BlueCheese.FX.Preview.AutoPlay";
		private const string _loopKey = "BlueCheese.FX.Preview.Loop";
		private const string _heightKey = "BlueCheese.FX.Preview.Height";

		public const float MinHeight = 120f;
		public const float MaxHeight = 800f;

		/// <summary>Start playing as soon as an FXDef is selected.</summary>
		public static bool AutoPlay
		{
			get => EditorPrefs.GetBool(_autoPlayKey, true);
			set => EditorPrefs.SetBool(_autoPlayKey, value);
		}

		/// <summary>
		/// Replay the effect indefinitely instead of pausing at the end of a single pass. Off by default so
		/// the preview settles on its own rather than simulating for as long as the inspector stays visible.
		/// </summary>
		public static bool Loop
		{
			get => EditorPrefs.GetBool(_loopKey, false);
			set => EditorPrefs.SetBool(_loopKey, value);
		}

		/// <summary>Height of the preview viewport, in points.</summary>
		public static float Height
		{
			get => Mathf.Clamp(EditorPrefs.GetFloat(_heightKey, 260f), MinHeight, MaxHeight);
			set => EditorPrefs.SetFloat(_heightKey, Mathf.Clamp(value, MinHeight, MaxHeight));
		}
	}
}
