//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public class UIService : IUIService
    {
        private readonly IAssetService _assetService;
        private readonly IInputService _inputService;
        private readonly IClockService _clockService;
        private readonly ILogger<UIService> _logger;
        private Dictionary<string, UIView> _viewPrefabs;
        private readonly List<UIView> _viewList = new();
        private UIView _currentView;
        private bool _isInitialized;

        public UIService(IAssetService assetService, IInputService inputService, IClockService clockService, ILogger<UIService> logger)
        {
            _assetService = assetService;
            _inputService = inputService;
            _clockService = clockService;
            _logger = logger;
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
            _viewPrefabs = new Dictionary<string, UIView>();
            UIViewBank[] banks = _assetService.LoadAssetsFromResources<UIViewBank>("UI");
            foreach (var bank in banks)
            {
                foreach (var view in bank.ViewPrefabs)
                {
                    _viewPrefabs.Add(view.name, view);
                }
            }
        }

        public UIView CreateView(string viewName, Transform parent = null)
        {
            if (_viewPrefabs.TryGetValue(viewName, out var prefab) == false)
            {
                _logger.LogWarning($"UIService - Unable to instantiate UIView with name: {viewName}");
            }

            return _assetService.Instantiate(prefab, parent: parent);
        }

        public void RegisterView(UIView view)
        {
            _viewList.Add(view);
            CheckCurrentView();
        }

        public void UnregisterView(UIView view)
        {
            _viewList.Remove(view);
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