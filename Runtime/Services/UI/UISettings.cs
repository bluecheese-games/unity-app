using BlueCheese.Core.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlueCheese.App
{
	/// <summary>
	/// Single, app-wide source of truth for UI-module-wide configuration, grouped into sections (one per
	/// concern) so new options can be added later without introducing a new AssetBank-registered asset type
	/// and a new DI registration each time -- just add a field/section here.
	/// </summary>
	[CreateAssetMenu(menuName = "BlueCheese/UI/UI Settings", fileName = "UISettings")]
	public class UISettings : AssetBase
	{
		/// <summary>
		/// How every <see cref="UIView"/>/<see cref="Popup"/>'s <see cref="CanvasScaler"/> should be
		/// configured. Applied to every spawned view's CanvasScaler by <see cref="UIService.SpawnView(UIViewDef)"/>
		/// (overriding whatever's saved on that particular prefab, so views/popups can never drift out of
		/// sync with each other), and mirrored onto <see cref="Popup"/> prefabs at edit time via
		/// <c>OnValidate</c> so they preview correctly without entering Play Mode.
		/// </summary>
		[Serializable]
		public class CanvasSection
		{
			public CanvasScaler.ScaleMode UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			public Vector2 ReferenceResolution = new(1920, 1080);
			public CanvasScaler.ScreenMatchMode ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			[Range(0f, 1f)] public float MatchWidthOrHeight = 0.5f;
			public float ReferencePixelsPerUnit = 100f;

			/// <summary>
			/// Copies this section's configuration onto the given <see cref="CanvasScaler"/>. Safe no-op if null.
			/// </summary>
			public void ApplyTo(CanvasScaler scaler)
			{
				if (scaler == null)
				{
					return;
				}

				scaler.uiScaleMode = UiScaleMode;
				scaler.referenceResolution = ReferenceResolution;
				scaler.screenMatchMode = ScreenMatchMode;
				scaler.matchWidthOrHeight = MatchWidthOrHeight;
				scaler.referencePixelsPerUnit = ReferencePixelsPerUnit;
			}
		}

		/// <summary>
		/// App-wide defaults for <see cref="UIButton"/>. Each nested section doubles as the payload type
		/// for <see cref="UIButton"/>'s own per-button <c>Optional&lt;T&gt;</c> overrides (see
		/// <see cref="UIButton"/>), so there is exactly one place that defines the shape of a feature's
		/// parameters. Add further sections here as new button-wide behaviors come up (navigation,
		/// cooldown, haptics...).
		/// </summary>
		[Serializable]
		public class ButtonSection
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
		}

		[Header("Canvas")]
		public CanvasSection Canvas = new();

		[Header("Button")]
		public ButtonSection Button = new();

		// Add further sections here as new UI-wide concerns come up, e.g.:
		// [Header("Transitions")] public TransitionSection Transitions = new();
		// [Header("Navigation")] public NavigationSection Navigation = new();

		public static UISettings FromAssetBankOrDefault()
		{
			var settings = AssetBank.GetAssetOfType<UISettings>();
			if (settings == null)
			{
				Debug.LogWarning("UISettings asset not found. Using default settings.");
				settings = CreateInstance<UISettings>();
			}
			return settings;
		}
	}
}
