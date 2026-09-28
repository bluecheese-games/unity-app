using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace BlueCheese.App
{
	/// <summary>
	/// Drop-in enrichment for a <see cref="Button"/>: a click punch animation, auto-grayscale of every
	/// graphic under it while non-interactable, a click SFX, and a free-form <see cref="Id"/> for anything
	/// external (analytics, automated tests, per-id SFX lookups...) that wants to identify this button.
	///
	/// All behavior is centrally configured via <see cref="UISettings"/>.<see cref="UISettings.Button"/>
	/// (registered by <see cref="DefaultServicesInstaller"/>); each feature can be overridden per-button
	/// through the <see cref="Optional{T}"/> fields below -- leave a field unchecked to inherit the
	/// app-wide default, tick it to replace that section wholesale for this one button. New button-wide
	/// features should follow the same recipe: add a section to <see cref="UISettings.ButtonSection"/>,
	/// then an <c>Optional&lt;ThatSection&gt;</c> override field here.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class UIButton : MonoBehaviour
	{
		private static Shader _grayscaleShader;

		[Tooltip("Free-form identifier for this button (analytics, automated tests, per-id SFX lookups...). Not used by UIButton itself.")]
		[SerializeField] private string _id;

		[SerializeField] private Optional<UISettings.ButtonSection.PunchSection> _punchOverride;
		[SerializeField] private Optional<UISettings.ButtonSection.GrayscaleSection> _grayscaleOverride;
		[SerializeField] private Optional<SoundFX> _sfxOverride;

		[Injectable] private IOptions<UISettings> _settings;

		private Button _button;
		private RectTransform _rectTransform;
		private Vector3 _originalScale;

		// Image/RawImage graphics get a real per-pixel desaturation via a shared shader (so multi-colored
		// sprites/icons actually turn gray, not just dimmer); everything else (TMP text, legacy Text...)
		// gets a cheap color-lerp-toward-luminance instead, since swapping their material would break
		// whatever SDF/font shader they depend on.
		private Graphic[] _imageGraphics;
		private Material[] _grayscaleMaterials;
		private Graphic[] _tintGraphics;
		private Color[] _originalTintColors;

		private bool? _wasInteractable;
		private CancellationTokenSource _punchCts;

		/// <summary> Free-form identifier for this button. See the field tooltip for intended uses. </summary>
		public string Id => _id;

		private UISettings.ButtonSection.PunchSection Punch => _punchOverride.Resolve(_settings.Value.Button.Punch);
		private UISettings.ButtonSection.GrayscaleSection Grayscale => _grayscaleOverride.Resolve(_settings.Value.Button.Grayscale);
		private SoundFX Sfx => _sfxOverride.Resolve(_settings.Value.Button.Audio.ClickSfx);

		private void Awake()
		{
			ServiceInjector.Inject(this);

			_button = GetComponent<Button>();
			_rectTransform = (RectTransform)transform;
			_originalScale = _rectTransform.localScale;

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

			_button.onClick.AddListener(OnClick);
		}

		private void OnEnable()
		{
			// Force a refresh next Update, in case interactable changed while this button was disabled.
			_wasInteractable = null;
		}

		private void Update()
		{
			// Selectable/Button raise no event when `interactable` changes, so this is the only reliable
			// way to react to it being toggled from anywhere (including plain `button.interactable = x`).
			if (_wasInteractable != _button.interactable)
			{
				_wasInteractable = _button.interactable;
				ApplyGrayscale(!_button.interactable);
			}
		}

		private void OnDestroy()
		{
			_punchCts?.Cancel();
			_punchCts?.Dispose();

			if (_button != null)
			{
				_button.onClick.RemoveListener(OnClick);
			}

			foreach (var material in _grayscaleMaterials)
			{
				if (material != null) Destroy(material);
			}
		}

		private void OnClick()
		{
			PlaySfx();
			PlayPunchAsync().Forget();
		}

		private void PlaySfx()
		{
			var sfx = Sfx;
			if (sfx.IsValid)
			{
				sfx.Play();
			}
		}

		private async UniTask PlayPunchAsync()
		{
			var punch = Punch;
			if (!punch.Enabled || punch.Duration <= 0f)
			{
				return;
			}

			_punchCts?.Cancel();
			_punchCts?.Dispose();
			_punchCts = new CancellationTokenSource();
			CancellationToken token = _punchCts.Token;

			Vector3 targetScale = _originalScale * punch.Scale;
			float elapsed = 0f;

			try
			{
				while (elapsed < punch.Duration)
				{
					elapsed += Time.unscaledDeltaTime;
					float progress = punch.Curve.Evaluate(Mathf.Clamp01(elapsed / punch.Duration));
					_rectTransform.localScale = Vector3.LerpUnclamped(targetScale, _originalScale, progress);
					await UniTask.Yield(PlayerLoopTiming.Update, token);
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}

			_rectTransform.localScale = _originalScale;
		}

		private void ApplyGrayscale(bool wantsGrayscale)
		{
			var grayscale = Grayscale;
			bool active = wantsGrayscale && grayscale.Enabled;
			float saturation = active ? grayscale.Saturation : 1f;

			for (int i = 0; i < _imageGraphics.Length; i++)
			{
				var graphic = _imageGraphics[i];
				if (graphic == null) continue;

				if (!active)
				{
					graphic.material = null; // restore the Graphic's own default material
					continue;
				}

				_grayscaleMaterials[i] ??= CreateGrayscaleMaterial();
				if (_grayscaleMaterials[i] == null) continue; // shader unavailable (e.g. stripped from build)

				_grayscaleMaterials[i].SetFloat("_Saturation", saturation);
				graphic.material = _grayscaleMaterials[i];
			}

			for (int i = 0; i < _tintGraphics.Length; i++)
			{
				var graphic = _tintGraphics[i];
				if (graphic == null) continue;

				Color original = _originalTintColors[i];
				if (!active)
				{
					graphic.color = original;
					continue;
				}

				float luminance = Vector3.Dot(new Vector3(original.r, original.g, original.b), new Vector3(0.299f, 0.587f, 0.114f));
				Color gray = new(luminance, luminance, luminance, original.a);
				graphic.color = Color.Lerp(gray, original, saturation);
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
