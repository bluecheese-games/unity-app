//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public class UIService : IUIService
    {
        private readonly IAssetService _assetService;
        private readonly IPoolService _poolService;
        private readonly IInputService _inputService;
        private readonly IClockService _clockService;
        private Dictionary<string, GameObject> _viewPrefabs;
        private readonly List<UIView> _viewList = new();
        private UIView _currentView;
        private bool _isInitialized;

        public UIService(IAssetService assetService, IPoolService poolService, IInputService inputService, IClockService clockService)
        {
            _assetService = assetService;
            _poolService = poolService;
            _inputService = inputService;
            _clockService = clockService;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            LoadViewsInResources();

            _clockService.OnTick += HandleTick;

            _isInitialized = true;
        }

        private void HandleTick(float deltaTime)
        {
            if (_currentView == null)
            {
                return;
            }

            if (_inputService.GetButtonDown("Cancel") || _inputService.GetKeyDown(KeyCode.Escape))
            {
                _currentView.HandleBackButton();
            }
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
            CheckCurrentView();
        }

        public void UnregisterView(UIView view)
        {
            _viewList.Remove(view);
            _poolService.Despawn(view.gameObject);
            CheckCurrentView();
        }

        private void CheckCurrentView()
        {
            if (_viewList.Count == 0)
            {
                _currentView = null;
            }
            else
            {
                _currentView = _viewList.Last();

                // Focus current view
                _currentView.Focus();
            }
        }
    }
}