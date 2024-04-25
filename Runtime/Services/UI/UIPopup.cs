//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    [RequireComponent(typeof(Canvas))]
    public class UIPopup : MonoBehaviour
    {
        // Save the top sorting order
        // TODO: fix domain reload
        private static int _topSortingOrder = 100;

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            _canvas.sortingOrder = ++_topSortingOrder;
        }

        private void OnDisable()
        {
            if (_canvas.sortingOrder >= _topSortingOrder)
            {
                _topSortingOrder = _canvas.sortingOrder - 1;
            }
        }
    }
}