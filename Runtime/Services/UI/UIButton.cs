using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueCheese.App
{
	/// <summary>
	/// Drop-in replacement for <see cref="Button"/> (same GameObject, same serialized data -- onClick,
	/// colors, transition, targetGraphic all carry over) that adds: a configurable press animation, a
	/// configurable visual treatment while non-interactable, a click SFX, and a free-form <see cref="Id"/>
	/// for anything external (analytics, automated tests, per-id SFX lookups...) that wants to identify
	/// this button.
	///
	/// Subclasses <see cref="Button"/> rather than sitting alongside it as a companion component so it can
	/// hook <see cref="DoStateTransition"/> -- the same mechanism Selectable itself uses to drive its own
	/// color/sprite/animation transitions -- instead of polling <c>interactable</c> every frame. That also
	/// means the disable-state visual reacts immediately and correctly whether <c>interactable</c> is
	/// flipped from code, from the Inspector while the game is running, or from a CanvasGroup up the
	/// hierarchy. Likewise, the press animation and click SFX both trigger together from
	/// <see cref="OnPointerDown"/>/<see cref="OnSubmit"/> (the moment of press, mouse or gamepad/keyboard,
	/// see <see cref="HandlePress"/>) rather than from <c>onClick</c> -- which fires on release, at the same
	/// instant a click handler might already be navigating away or opening a popover, leaving no visible
	/// frame for the animation to play before the button itself is affected, and which would otherwise fire
	/// the SFX out of sync with an animation triggered on press.
	///
	/// All behavior is centrally configured via <see cref="UISettings"/>.<see cref="UISettings.Button"/>
	/// (registered by <see cref="DefaultServicesInstaller"/>); each feature can be overridden per-button
	/// through the <see cref="Optional{T}"/> fields below -- leave a field unchecked to inherit the
	/// app-wide default, tick it to replace that section wholesale for this one button. New button-wide
	/// features should follow the same recipe: add a section to <see cref="UISettings.ButtonSection"/>,
	/// then an <c>Optional&lt;ThatSection&gt;</c> override field here.
	/// </summary>
	public class UIButton : Button
	{
		private static Shader _grayscaleShader;

		[Tooltip("Free-form identifier for this button (analytics, automated tests, per-id SFX lookups...). Not used by UIButton itself.")]
		[SerializeField] private string _id;

		[SerializeField] private Optional<UISettings.ButtonSection.AnimationSection> _animationOverride;
		[SerializeField] private Optional<UISettings.ButtonSection.DisableStateSection> _disableStateOverride;
		[SerializeField] private Optional<SoundFX> _sfxOverride;

		[Injectable] private IOptions<UISettings> _settings;

		private RectTransform _rectTransform;
		private Vector3 _originalScale;
		private Vector2 _originalAnchoredPosition;
		private CanvasGroup _canvasGroup;
		private bool? _lastDisabledState;

		// Image/RawImage graphics get a real per-pixel lerp-to-flat-gray via a shared shader (so
		// multi-colored sprites/icons, and pure whites/blacks, actually turn gray at amount=1, not just
		// dimmer); everything else (TMP text, legacy Text...) gets a cheap color-lerp instead, since
		// swapping their material would break whatever SDF/font shader they depend on. Only used by
		// DisableStateMode.Grayscale.
		private bool _initialized;
		private Graphic[] _imageGraphics;
		private Material[] _grayscaleMaterials;
		private Graphic[] _tintGraphics;
		private Color[] _originalTintColors;

		private CancellationTokenSource _animCts;

		/// <summary> Free-form identifier for this button. See the field tooltip for intended uses. </summary>
		public string Id => _id;

		private UISettings.ButtonSection.AnimationSection Animation => _animationOverride.Resolve(_settings.Value.Button.Animation);
		private UISettings.ButtonSection.DisableStateSection DisableState => _disableStateOverride.Resolve(_settings.Value.Button.DisableState);
		private SoundFX Sfx => _sfxOverride.Resolve(_settings.Value.Button.Audio.ClickSfx);

		protected override void Awake()
		{
			base.Awake();
			if (Application.isPlaying) EnsureInitialized();
		}

		/// <summary>
		/// Lazily resolves DI/gathers graphics on first real use rather than solely in <see cref="Awake"/>.
		/// Awake() alone isn't reliable here: it doesn't fire again on entering Play Mode for objects that
		/// already existed in the open scene when the project has Edit &gt; Project Settings &gt; Editor &gt;
		/// Enter Play Mode Options set to skip domain/scene reload (a common iteration-speed optimization)
		/// -- exactly the case for a button whose script/state was set up via editor tooling before Play
		/// was ever pressed. Called defensively from every entry point that needs <see cref="_settings"/>
		/// or the graphics cache, so initialization happens on whichever comes first.
		/// </summary>
		private void EnsureInitialized()
		{
			if (_initialized) return;
			_initialized = true;

			ServiceInjector.Inject(this);

			_rectTransform = (RectTransform)transform;
			_originalScale = _rectTransform.localScale;
			_originalAnchoredPosition = _rectTransform.anchoredPosition;
			GatherGraphics();
		}

		protected override void OnDestroy()
		{
			_animCts?.Cancel();
			_animCts?.Dispose();

			if (_grayscaleMaterials != null)
			{
				foreach (var material in _grayscaleMaterials)
				{
					if (material != null) Destroy(material);
				}
			}

			base.OnDestroy();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			if (!Application.isPlaying) return;
			EnsureInitialized();
			HandlePress();
		}

		public override void OnSubmit(BaseEventData eventData)
		{
			base.OnSubmit(eventData);
			if (!Application.isPlaying) return;
			EnsureInitialized();
			HandlePress();
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			base.DoStateTransition(state, instant);
			if (!Application.isPlaying) return;

			EnsureInitialized();
			ApplyDisableState(state == SelectionState.Disabled);
		}

		/// <summary>
		/// Fires the click SFX and the press animation from the same call, at the moment of press -- not
		/// from <c>onClick</c> (fires on release) and not from two separate trigger points, either of which
		/// would let the sound and the animation drift out of sync with each other by a frame or more.
		/// </summary>
		private void HandlePress()
		{
			if (!IsActive() || !IsInteractable()) return;
			PlaySfx();
			PlayAnimationAsync().Forget();
		}

		private void GatherGraphics()
		{
			var imageGraphics = new List<Graphic>();
			var tintGraphics = new List<Graphic>();
			foreach (var graphic in GetComponentsInChildren<Graphic>(true))
			{
				if (graphic is Image or RawImage) imageGraphics.Add(graphic);
				else tintGraphics.Add(graphic);
			}
			_imageGraphics = imageGraphics.ToArray();
			_grayscaleMaterials = new Material[_imageGraphics.Length];
			_tintGraphics = tintGraphics.ToArray();
			_originalTintColors = new Color[_tintGraphics.Length];
			for (int i = 0; i < _tintGraphics.Length; i++)
			{
				_originalTintColors[i] = _tintGraphics[i].color;
			}
		}

		private void PlaySfx()
		{
			var sfx = Sfx;
			if (sfx.IsValid)
			{
				sfx.Play();
			}
		}

		/// <summary>
		/// Plays the press animation according to <see cref="Animation"/>'s <see cref="UISettings.ButtonSection.AnimationType"/>.
		/// Punch Down/Punch Up animate a single scale channel (jump to <c>PunchScale</c>, spring back to the
		/// original scale). Custom independently animates whichever of Scale/OffsetX/OffsetY channels are
		/// enabled, each via its own <see cref="CurveParam.Curve"/>, sharing the same Duration.
		/// </summary>
		private async UniTask PlayAnimationAsync()
		{
			var anim = Animation;
			if (anim.Type == UISettings.ButtonSection.AnimationType.None || anim.Duration <= 0f)
			{
				return;
			}

			bool animateScale = false, animateX = false, animateY = false;
			float scaleFactor = 1f, offsetX = 0f, offsetY = 0f;
			AnimationCurve scaleCurve = anim.Curve, xCurve = anim.Curve, yCurve = anim.Curve;

			switch (anim.Type)
			{
				case UISettings.ButtonSection.AnimationType.PunchDown:
				case UISettings.ButtonSection.AnimationType.PunchUp:
					animateScale = true;
					scaleFactor = anim.PunchScale;
					break;
				case UISettings.ButtonSection.AnimationType.Custom:
					var custom = anim.Custom;
					if (custom.Scale.Enabled) { animateScale = true; scaleFactor = custom.Scale.Value.Value; scaleCurve = custom.Scale.Value.Curve; }
					if (custom.OffsetX.Enabled) { animateX = true; offsetX = custom.OffsetX.Value.Value; xCurve = custom.OffsetX.Value.Curve; }
					if (custom.OffsetY.Enabled) { animateY = true; offsetY = custom.OffsetY.Value.Value; yCurve = custom.OffsetY.Value.Curve; }
					break;
			}

			if (!animateScale && !animateX && !animateY)
			{
				return;
			}

			_animCts?.Cancel();
			_animCts?.Dispose();
			_animCts = new CancellationTokenSource();
			CancellationToken token = _animCts.Token;

			Vector3 scaleFrom = _originalScale * scaleFactor;
			Vector2 posFrom = _originalAnchoredPosition + new Vector2(animateX ? offsetX : 0f, animateY ? offsetY : 0f);

			try
			{
				await UIAnimationUtility.RunAsync(anim.Duration, token, progress =>
				{
					if (animateScale)
					{
						_rectTransform.localScale = Vector3.LerpUnclamped(scaleFrom, _originalScale, scaleCurve.Evaluate(progress));
					}
					if (animateX || animateY)
					{
						Vector2 pos = _rectTransform.anchoredPosition;
						if (animateX) pos.x = Mathf.LerpUnclamped(posFrom.x, _originalAnchoredPosition.x, xCurve.Evaluate(progress));
						if (animateY) pos.y = Mathf.LerpUnclamped(posFrom.y, _originalAnchoredPosition.y, yCurve.Evaluate(progress));
						_rectTransform.anchoredPosition = pos;
					}
				});
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (animateScale) _rectTransform.localScale = _originalScale;
			if (animateX || animateY) _rectTransform.anchoredPosition = _originalAnchoredPosition;
		}

		private void ApplyDisableState(bool disabled)
		{
			if (_lastDisabledState == disabled) return;
			_lastDisabledState = disabled;

			ResetAlpha();
			ResetGrayscale();

			if (!disabled) return;

			var state = DisableState;
			switch (state.Mode)
			{
				case UISettings.ButtonSection.DisableStateMode.Alpha:
					_canvasGroup = _canvasGroup != null ? _canvasGroup : GetOrAddCanvasGroup();
					_canvasGroup.alpha = state.DisabledAlpha;
					break;
				case UISettings.ButtonSection.DisableStateMode.Grayscale:
					ApplyGrayscale(state.GrayscaleAmount);
					break;
				case UISettings.ButtonSection.DisableStateMode.Default:
				default:
					break; // Leave Button's own Transition/ColorBlock as the only visual cue.
			}
		}

		private CanvasGroup GetOrAddCanvasGroup() => TryGetComponent(out CanvasGroup group) ? group : gameObject.AddComponent<CanvasGroup>();

		private void ResetAlpha()
		{
			if (_canvasGroup != null) _canvasGroup.alpha = 1f;
		}

		private void ResetGrayscale()
		{
			for (int i = 0; i < _imageGraphics.Length; i++)
			{
				if (_imageGraphics[i] != null) _imageGraphics[i].material = null;
			}
			for (int i = 0; i < _tintGraphics.Length; i++)
			{
				if (_tintGraphics[i] != null) _tintGraphics[i].color = _originalTintColors[i];
			}
		}

		// Flat mid-gray target for both the shader path (Image/RawImage) and the tint-color path (TMP/legacy
		// Text below): at amount=1 every graphic becomes exactly this gray, regardless of its original color
		// -- including pure white and pure black, which a luminance-preserving desaturation would leave
		// untouched. At amount=0 the original color/texture is unaffected.
		private static readonly Color _flatGray = new(0.5f, 0.5f, 0.5f, 1f);

		private void ApplyGrayscale(float amount)
		{
			for (int i = 0; i < _imageGraphics.Length; i++)
			{
				var graphic = _imageGraphics[i];
				if (graphic == null) continue;

				_grayscaleMaterials[i] ??= CreateGrayscaleMaterial();
				if (_grayscaleMaterials[i] == null) continue; // shader unavailable (e.g. stripped from build)

				_grayscaleMaterials[i].SetFloat("_GrayscaleAmount", amount);
				graphic.material = _grayscaleMaterials[i];
			}

			for (int i = 0; i < _tintGraphics.Length; i++)
			{
				var graphic = _tintGraphics[i];
				if (graphic == null) continue;

				Color original = _originalTintColors[i];
				Color gray = new(_flatGray.r, _flatGray.g, _flatGray.b, original.a);
				graphic.color = Color.Lerp(original, gray, amount);
			}
		}

		private static Material CreateGrayscaleMaterial()
		{
			if (_grayscaleShader == null)
			{
				_grayscaleShader = Shader.Find("BlueCheese/UI/Grayscale");
			}
			return _grayscaleShader != null ? new Material(_grayscaleShader) : null;
		}
	}
}
