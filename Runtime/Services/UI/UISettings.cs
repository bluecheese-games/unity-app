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
			public enum AnimationType
			{
				/// <summary>No press animation.</summary>
				None,
				/// <summary>Button shrinks to <see cref="AnimationSection.PunchScale"/> the instant it's
				/// pressed, then springs back to its normal size. Use a <see cref="AnimationSection.PunchScale"/>
				/// below 1.</summary>
				PunchDown,
				/// <summary>Same mechanism as <see cref="PunchDown"/>, but grows instead of shrinking. Use a
				/// <see cref="AnimationSection.PunchScale"/> above 1.</summary>
				PunchUp,
				/// <summary>Freely combine an optional scale and/or X/Y position offset, each with its own
				/// curve, via <see cref="AnimationSection.Custom"/>.</summary>
				Custom,
			}

			/// <summary>
			/// A single Custom-mode animation channel (scale, or X/Y offset in local pixels), each
			/// individually toggleable and driven by its own <see cref="CurveParam.Curve"/>.
			/// </summary>
			[Serializable]
			public class CustomAnimationSection
			{
				public Optional<CurveParam> Scale = new(new CurveParam { Value = 0.9f }, false);
				public Optional<CurveParam> OffsetX = new(new CurveParam { Value = 10f }, false);
				public Optional<CurveParam> OffsetY = new(new CurveParam { Value = 10f }, false);
			}

			[Serializable]
			public class AnimationSection
			{
				public AnimationType Type = AnimationType.PunchDown;
				[Min(0f)] public float Duration = 0.12f;
				[Tooltip("Scale multiplier applied the instant the button is pressed, animating back to the " +
					"button's normal scale over Duration. Used by Punch Down/Punch Up only -- use a value " +
					"below 1 for Punch Down, above 1 for Punch Up.")]
				[Min(0.01f)] public float PunchScale = 0.9f;
				[Tooltip("Used by Punch Down/Punch Up only. Custom mode uses each channel's own curve instead.")]
				public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
				public CustomAnimationSection Custom = new();
			}

			public enum DisableStateMode
			{
				/// <summary>Do nothing extra -- leave whatever the Button's own Transition/ColorBlock does.</summary>
				Default,
				/// <summary>Desaturate every graphic under the button towards flat gray (see <see cref="UIButton"/>).</summary>
				Grayscale,
				/// <summary>Fade the whole button out via a CanvasGroup.</summary>
				Alpha,
			}

			[Serializable]
			public class DisableStateSection
			{
				public DisableStateMode Mode = DisableStateMode.Grayscale;
				[Tooltip("0 = untouched colors, 1 = every graphic (including whites/blacks) becomes flat gray.")]
				[Range(0f, 1f)] public float GrayscaleAmount = 1f;
				[Range(0f, 1f)] public float DisabledAlpha = 0.5f;
			}

			[Serializable]
			public class AudioSection
			{
				public SoundFX ClickSfx;
			}

			[Header("Animation")] public AnimationSection Animation = new();
			[Header("Disable state")] public DisableStateSection DisableState = new();
			[Header("Audio")] public AudioSection Audio = new();
		}

		/// <summary>
		/// App-wide defaults for <see cref="Popup"/>'s show/hide transition (see <see cref="PopupToggleableView"/>),
		/// mirroring <see cref="ButtonSection"/>'s Custom-channel recipe: pick a preset <see cref="TransitionType"/>,
		/// or Custom to freely combine scale/alpha/position-offset channels each with their own curve.
		/// </summary>
		[Serializable]
		public class PopupSection
		{
			public enum TransitionType
			{
				None,
				/// <summary>Fades in/out via a CanvasGroup.</summary>
				Fade,
				/// <summary>Fades in/out while scaling from <see cref="TransitionSection.StartScale"/> to 1.</summary>
				ScaleFade,
				SlideFromBottom,
				SlideFromTop,
				SlideFromLeft,
				SlideFromRight,
				/// <summary>Freely combine an optional scale, alpha, and/or X/Y position offset, each with its
				/// own curve, via <see cref="TransitionSection.Custom"/>.</summary>
				Custom,
			}

			[Serializable]
			public class CustomTransitionSection
			{
				public Optional<CurveParam> Scale = new(new CurveParam { Value = 0.85f }, false);
				public Optional<CurveParam> Alpha = new(new CurveParam { Value = 0f }, false);
				public Optional<CurveParam> OffsetX = new(new CurveParam { Value = 0f }, false);
				public Optional<CurveParam> OffsetY = new(new CurveParam { Value = -400f }, false);
			}

			[Serializable]
			public class TransitionSection
			{
				public TransitionType Type = TransitionType.ScaleFade;
				[Min(0f)] public float Duration = 0.2f;
				[Tooltip("Used by every preset type; Custom mode uses each channel's own curve instead.")]
				public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
				[Tooltip("Used by Scale Fade only.")]
				[Min(0.01f)] public float StartScale = 0.85f;
				[Tooltip("Used by Slide From */Custom offset channels only, in local pixels.")]
				public float SlideDistance = 400f;
				public CustomTransitionSection Custom = new();
			}

			[Serializable]
			public class AudioSection
			{
				public SoundFX OpenSfx;
				public SoundFX CloseSfx;
			}

			[Header("Animation in")] public TransitionSection AnimIn = new();
			[Header("Animation out")] public TransitionSection AnimOut = new();
			[Header("Audio")] public AudioSection Audio = new();
		}

		[Header("Canvas")]
		public CanvasSection Canvas = new();

		[Header("Button")]
		public ButtonSection Button = new();

		[Header("Popup")]
		public PopupSection Popup = new();

		// Add further sections here as new UI-wide concerns come up, e.g.:
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
