//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
    [RequireComponent(typeof(Canvas), typeof(UIView))]
    public class Popup : MonoBehaviour
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
        private UIView _uiView;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _uiView = GetComponent<UIView>();
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

        public async Task<PopupResult> ShowAsync()
        {
            gameObject.SetActive(true);
            while(gameObject.activeSelf)
            {
                await Task.Yield();
            }
            return Result;
        }

        public void SetResult(PopupResult result)
        {
            Result = result;
            _uiView.Hide();
        }

        public void Ok() => SetResult(PopupResult.Ok);
        public void Cancel() => SetResult(PopupResult.Cancel);
    }
}