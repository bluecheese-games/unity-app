using BlueCheese.Core.Utils;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// App-wide defaults for <see cref="UIButton"/>, grouped into sections so new button-wide behaviors
	/// (navigation, cooldown, haptics...) can be added later without a new AssetBank-registered asset type
	/// each time -- just add a field/section here, following the same pattern as <see cref="UISettings"/>/
	/// <see cref="AudioSettings"/>. Each section doubles as the payload type for <see cref="UIButton"/>'s
	/// own per-button <c>Optional&lt;T&gt;</c> overrides (see <see cref="UIButton"/>), so there is exactly
	/// one place that defines the shape of a feature's parameters.
	/// </summary>
	[CreateAssetMenu(menuName = "BlueCheese/UI/Button Settings", fileName = "ButtonSettings")]
	public class ButtonSettings : AssetBase
	{
		[Serializable]
		public class PunchSection
		{
			public bool Enabled = true;
			[Min(0.01f)] public float Scale = 0.9f;
			[Min(0f)] public float Duration = 0.12f;
			public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		}

		[Serializable]
		public class GrayscaleSection
		{
			public bool Enabled = true;
			[Range(0f, 1f)] public float Saturation = 0f;
		}

		[Serializable]
		public class AudioSection
		{
			public SoundFX ClickSfx;
		}

		[Header("Punch animation")] public PunchSection Punch = new();
		[Header("Grayscale when disabled")] public GrayscaleSection Grayscale = new();
		[Header("Audio")] public AudioSection Audio = new();

		// Add further sections here as new button-wide behaviors come up, e.g.:
		// [Header("Navigation")] public NavigationSection Navigation = new();

		public static ButtonSettings FromAssetBankOrDefault()
		{
			var settings = AssetBank.GetAssetOfType<ButtonSettings>();
			if (settings == null)
			{
				Debug.LogWarning("ButtonSettings asset not found. Using default settings.");
				settings = CreateInstance<ButtonSettings>();
			}
			return settings;
		}
	}
}
