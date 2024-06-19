//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App.Services
{
    [RequireComponent(typeof(Canvas))]
    public class UIPopup : MonoBehaviour
    {
        // Save the top sorting order
        // TODO: fix domain reload
        private static int _topSortingOrder = 100;

        [SerializeField] private PopupResult _defaultResult;

        public PopupResult Result => _result == PopupResult.None ? _defaultResult : _result;

        private Canvas _canvas;
        private PopupResult _result = PopupResult.None;
        private bool _hidden = true;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            _canvas.sortingOrder = ++_topSortingOrder;
            _hidden = false;
        }

        private void OnDisable()
        {
            if (_canvas.sortingOrder >= _topSortingOrder)
            {
                _topSortingOrder = _canvas.sortingOrder - 1;
            }
            _hidden = true;
        }

        public async Task WaitUntilHidden()
        {
            while(!_hidden)
            {
                await Task.Yield();
            }
        }

        public void SetResult(PopupResult result) => _result = result;
        public void SetResult_None() => SetResult(PopupResult.None);
        public void SetResult_Ok() => SetResult(PopupResult.Ok);
        public void SetResult_Cancel() => SetResult(PopupResult.Cancel);
        public void SetResult_Yes() => SetResult(PopupResult.Yes);
        public void SetResult_No() => SetResult(PopupResult.No);

    }
}