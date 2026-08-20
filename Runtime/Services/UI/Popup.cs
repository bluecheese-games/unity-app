//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BlueCheese.App
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class Popup : UIViewBehaviour
	{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            _topSortingOrder = 100;
        }

        // Save the top sorting order
        private static int _topSortingOrder = 100;

        [SerializeField] private PopupResult _defaultResult;

        public PopupResult Result { get; private set; }

        private Canvas _canvas;

        // Completed when SetResult() (or Ok()/Cancel()) is called while a ShowAsync() awaiter is pending.
        //
        // This replaces a previous `while (gameObject.activeSelf) await UniTask.Yield();` polling loop,
        // which had a real bug: Popup requires a Canvas, so UIViewBehaviour always attaches a
        // CanvasToggleableView to it (see UIViewBehaviour.CreateAppropriateToggleableView), and that
        // implementation toggles Canvas.enabled rather than deactivating the GameObject. `activeSelf`
        // therefore never became false, and ShowAsync() would hang forever on every real Popup. Completing
        // an explicit awaiter on SetResult sidesteps GameObject/Canvas state entirely and also avoids the
        // per-frame Yield polling cost while the popup is open.
        private UniTaskCompletionSource<PopupResult> _resultCompletionSource;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            ValidateCanvasSetup();
        }

        // Popups stack via _canvas.sortingOrder, which only behaves as a "topmost popup wins" ordering for
        // Screen Space - Overlay/Camera canvases; each popup is also its own independent top-level canvas
        // (see UIViewBehaviour.CreateAppropriateToggleableView). World Space renders at a fixed world-unit
        // size (no screen-size scaling applies) and usually looks far too large or too small -- unlike the
        // CanvasScaler's own configuration (see UISettings.Canvas, applied here at edit time via OnValidate
        // below and again by UIService.SpawnView at runtime, regardless of what's saved on the prefab), a
        // World Space render mode can't be safely auto-corrected here (it may be an intentional, diegetic
        // choice for a specific view), so this only warns loudly instead of leaving it as a silent "why is
        // my popup huge" mystery.
        private void ValidateCanvasSetup()
        {
            if (_canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning(
                    $"[Popup] '{name}' has its Canvas set to World Space. Popups are stacked via " +
                    "sortingOrder, which is designed for Screen Space - Overlay/Camera canvases; World " +
                    "Space renders at a fixed world-unit size (no screen-size scaling applies) and usually " +
                    "looks far too large or too small. Set Canvas > Render Mode to Screen Space - Overlay.",
                    this);
            }
        }

#if UNITY_EDITOR
        // Keeps the prefab's own CanvasScaler in sync with the app-wide UISettings.Canvas section at edit
        // time, so a popup previews (and looks correct in the Scene/Game view) without needing Play Mode --
        // UIService.SpawnView applies the same section again at runtime regardless, so this is purely for a
        // truthful editing experience; it's not the thing that guarantees runtime correctness.
        private void OnValidate()
        {
            if (!TryGetComponent<CanvasScaler>(out var scaler))
            {
                return;
            }

            var settings = AssetBank.GetAssetOfType<UISettings>();
            if (settings == null)
            {
                return;
            }

            settings.Canvas.ApplyTo(scaler);
            UnityEditor.EditorUtility.SetDirty(scaler);
        }
#endif

        private void OnEnable()
        {
            _canvas.sortingOrder = ++_topSortingOrder;
            Result = _defaultResult;
        }

        private void OnDisable()
        {
            if (_canvas.sortingOrder >= _topSortingOrder)
            {
                _topSortingOrder = _canvas.sortingOrder - 1;
            }

            // Defensive: resolves any pending ShowAsync() awaiter if the GameObject is deactivated without
            // SetResult/Ok/Cancel completing it first (e.g. a parent view hidden via SetActive, or the
            // popup destroyed outright). For a pool-spawned popup (the common case) this now also fires as
            // part of SetResult's own Hide -> ReturnToPoolIfPooled -> SetActive(false) chain (see
            // ToggleableView) -- harmless, since _resultCompletionSource is already null by the time
            // SetResult's own TrySetResult call below would otherwise run.
            _resultCompletionSource?.TrySetResult(Result);
            _resultCompletionSource = null;
        }

		public void Show() => ToggleableView.Toggle(true);

		public void Hide() => ToggleableView.Toggle(false);

		/// <summary>
		/// Shows the popup, awaits its show transition, then waits until <see cref="SetResult"/> (or
		/// <see cref="Ok"/>/<see cref="Cancel"/>) is called.
		/// </summary>
		public async UniTask<PopupResult> ShowAsync()
        {
			// Resolve any stale awaiter from a previous ShowAsync() call that never completed normally,
			// so it doesn't hang forever once replaced below.
			_resultCompletionSource?.TrySetResult(Result);
			_resultCompletionSource = new UniTaskCompletionSource<PopupResult>();

			await ToggleableView.ToggleAsync(true);

			return await _resultCompletionSource.Task;
        }

        public void SetResult(PopupResult result)
        {
            Result = result;
            ToggleableView.Toggle(false);
            _resultCompletionSource?.TrySetResult(result);
            _resultCompletionSource = null;
		}

        public void Ok() => SetResult(PopupResult.Ok);

        public void Cancel() => SetResult(PopupResult.Cancel);
    }
}