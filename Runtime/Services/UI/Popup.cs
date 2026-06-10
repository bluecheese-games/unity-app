//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
    [RequireComponent(typeof(Canvas))]
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

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

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
        }

		public void Show() => ToggleableView.Toggle(true);

		public void Hide() => ToggleableView.Toggle(false);

		public async UniTask<PopupResult> ShowAsync()
        {
			ToggleableView.Toggle(true);
			while (gameObject.activeSelf)
            {
                await UniTask.Yield();
            }
            return Result;
        }

        public void SetResult(PopupResult result)
        {
            Result = result;
            ToggleableView.Toggle(false);
		}

        public void Ok() => SetResult(PopupResult.Ok);

        public void Cancel() => SetResult(PopupResult.Cancel);
    }
}