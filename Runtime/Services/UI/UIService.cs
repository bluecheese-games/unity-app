//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
    public class UIService : IUIService
    {
        private readonly IAssetService _assetService;
        private readonly IPoolService _poolService;
        private Dictionary<string, GameObject> _viewPrefabs;
        private readonly List<UIView> _viewList = new();
        private UIView _currentView;
        private bool _isInitialized;

        public UIService(IAssetService assetService, IPoolService poolService)
        {
            _assetService = assetService;
            _poolService = poolService;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            LoadViewsInResources();

            _isInitialized = true;
        }

        private void LoadViewsInResources()
        {
            _viewPrefabs = new();
            UIViewBank[] banks = _assetService.LoadAssetsFromResources<UIViewBank>("UI");
            foreach (var bank in banks)
            {
                foreach (var view in bank.ViewPrefabs)
                {
                    _viewPrefabs.Add(view.name, view.gameObject);
                }
            }
        }

        public UIView CreateView(string viewName)
        {
            if (_viewPrefabs.TryGetValue(viewName, out var prefab) == false)
            {
                throw new Exception($"Unable to instantiate UIView with name: {viewName}");
            }

            return _poolService
                .Spawn(prefab)
                .GetComponent<UIView>();
        }

        public void RegisterView(UIView view)
        {
            _viewList.Add(view);
            UpdateCurrentView();
        }

        public void UnregisterView(UIView view)
        {
            _viewList.Remove(view);
            _poolService.Despawn(view.gameObject);
            UpdateCurrentView();
        }

        private void UpdateCurrentView()
        {
            if (_currentView != null)
            {
                _currentView.Focus(false);
            }

            if (_viewList.Count == 0)
            {
                _currentView = null;
            }
            else
            {
                _currentView = _viewList.Last();

                // Focus current view
                _currentView.Focus(true);
            }
        }
    }
}