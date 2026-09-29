using BlueCheese.Core.DI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// <see cref="CanvasToggleableView"/> subclass that <see cref="Popup"/> adds to itself (see
	/// <see cref="Popup.Awake"/>) instead of the plain <see cref="CanvasToggleableView"/>, so every popup gets
	/// a configurable enter/exit animation plus open/close SFX for free via <see cref="UISettings.Popup"/>,
	/// without each popup prefab needing its own bespoke transition script. To opt a specific popup prefab
	/// out of this (e.g. to author a fully custom transition instead), add a different
	/// <see cref="ToggleableView"/> subclass explicitly on the prefab -- see <see cref="UIViewBehaviour"/>'s
	/// remarks on implicit component wiring; Popup only adds this one if none is present yet.
	/// </summary>
	public class PopupToggleableView : CanvasToggleableView
	{
		[Injectable] private IOptions<UISettings> _settings;

		private RectTransform _rectTransform;
		private CanvasGroup _canvasGroup;
		private Vector2 _originalAnchoredPosition;
		private Vector3 _originalScale;
		private bool _initialized;

		private void EnsureInitialized()
		{
			if (_initialized) return;
			_initialized = true;

			ServiceInjector.Inject(this);

			_rectTransform = (RectTransform)transform;
			_originalAnchoredPosition = _rectTransform.anchoredPosition;
			_originalScale = _rectTransform.localScale;

			if (!TryGetComponent(out _canvasGroup))
			{
				_canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
		}

		protected override async UniTask PlayShowTransitionAsync()
		{
			// Same guard as UIButton's press animation/disable-state (see its remarks): skip entirely outside
			// Play Mode. Without this, ShowAsync()/HideAsync() -- called e.g. from AddComponent<Popup>() or a
			// bare unit test that never enters Play Mode -- would resolve the DI-guaranteed default UISettings
			// (ServiceContainer.ResolveOptions() always returns *some* instance, even unregistered) and start
			// a real, timed animation whose UniTask.Yield(PlayerLoopTiming.Update) never gets pumped outside
			// an actually-running Play Mode/Editor update loop, hanging the awaiting call forever.
			if (!Application.isPlaying) return;

			EnsureInitialized();

			var sfx = _settings.Value.Popup.Audio.OpenSfx;
			if (sfx.IsValid) sfx.Play();

			await PlayTransitionAsync(_settings.Value.Popup.AnimIn, entering: true);
		}

		protected override async UniTask PlayHideTransitionAsync()
		{
			if (!Application.isPlaying) return;

			EnsureInitialized();

			var sfx = _settings.Value.Popup.Audio.CloseSfx;
			if (sfx.IsValid) sfx.Play();

			await PlayTransitionAsync(_settings.Value.Popup.AnimOut, entering: false);
		}

		/// <summary>
		/// Animates whichever of scale/alpha/X-offset/Y-offset channels the given section's
		/// <see cref="UISettings.PopupSection.TransitionType"/> implies (or, for Custom, whichever of its
		/// channels are individually enabled). <paramref name="entering"/> flips the direction: true animates
		/// from the section's "from" state up to the popup's normal resting state (show), false animates from
		/// the resting state down to the "from" state (hide).
		/// </summary>
		private async UniTask PlayTransitionAsync(UISettings.PopupSection.TransitionSection section, bool entering)
		{
			if (section.Type == UISettings.PopupSection.TransitionType.None || section.Duration <= 0f)
			{
				_rectTransform.localScale = _originalScale;
				_rectTransform.anchoredPosition = _originalAnchoredPosition;
				_canvasGroup.alpha = 1f;
				return;
			}

			bool animateScale = false, animateAlpha = false, animateX = false, animateY = false;
			float scaleFrom = 1f, alphaFrom = 0f, offsetXFrom = 0f, offsetYFrom = 0f;
			AnimationCurve scaleCurve = section.Curve, alphaCurve = section.Curve, xCurve = section.Curve, yCurve = section.Curve;

			switch (section.Type)
			{
				case UISettings.PopupSection.TransitionType.Fade:
					animateAlpha = true;
					alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.ScaleFade:
					animateScale = true;
					scaleFrom = section.StartScale;
					animateAlpha = true;
					alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.SlideFromBottom:
					animateY = true; offsetYFrom = -section.SlideDistance;
					animateAlpha = true; alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.SlideFromTop:
					animateY = true; offsetYFrom = section.SlideDistance;
					animateAlpha = true; alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.SlideFromLeft:
					animateX = true; offsetXFrom = -section.SlideDistance;
					animateAlpha = true; alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.SlideFromRight:
					animateX = true; offsetXFrom = section.SlideDistance;
					animateAlpha = true; alphaFrom = 0f;
					break;
				case UISettings.PopupSection.TransitionType.Custom:
					var custom = section.Custom;
					if (custom.Scale.Enabled) { animateScale = true; scaleFrom = custom.Scale.Value.Value; scaleCurve = custom.Scale.Value.Curve; }
					if (custom.Alpha.Enabled) { animateAlpha = true; alphaFrom = custom.Alpha.Value.Value; alphaCurve = custom.Alpha.Value.Curve; }
					if (custom.OffsetX.Enabled) { animateX = true; offsetXFrom = custom.OffsetX.Value.Value; xCurve = custom.OffsetX.Value.Curve; }
					if (custom.OffsetY.Enabled) { animateY = true; offsetYFrom = custom.OffsetY.Value.Value; yCurve = custom.OffsetY.Value.Curve; }
					break;
			}

			if (!animateScale && !animateAlpha && !animateX && !animateY)
			{
				return;
			}

			Vector3 scaleStart = entering ? _originalScale * scaleFrom : _originalScale;
			Vector3 scaleEnd = entering ? _originalScale : _originalScale * scaleFrom;
			float alphaStart = entering ? alphaFrom : 1f;
			float alphaEnd = entering ? 1f : alphaFrom;
			Vector2 offsetPos = _originalAnchoredPosition + new Vector2(offsetXFrom, offsetYFrom);
			Vector2 posStart = entering ? offsetPos : _originalAnchoredPosition;
			Vector2 posEnd = entering ? _originalAnchoredPosition : offsetPos;

			try
			{
				await UIAnimationUtility.RunAsync(section.Duration, gameObject.GetCancellationTokenOnDestroy(), progress =>
				{
					if (animateScale)
					{
						_rectTransform.localScale = Vector3.LerpUnclamped(scaleStart, scaleEnd, scaleCurve.Evaluate(progress));
					}
					if (animateAlpha)
					{
						_canvasGroup.alpha = Mathf.LerpUnclamped(alphaStart, alphaEnd, alphaCurve.Evaluate(progress));
					}
					if (animateX || animateY)
					{
						Vector2 pos = _rectTransform.anchoredPosition;
						if (animateX) pos.x = Mathf.LerpUnclamped(posStart.x, posEnd.x, xCurve.Evaluate(progress));
						if (animateY) pos.y = Mathf.LerpUnclamped(posStart.y, posEnd.y, yCurve.Evaluate(progress));
						_rectTransform.anchoredPosition = pos;
					}
				});
			}
			catch (System.OperationCanceledException)
			{
				// Popup GameObject destroyed mid-transition -- nothing left to snap to a final state.
			}
		}
	}
}
