//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.App
{
    public class UIService : IUIService
    {
        private readonly IAssetLoaderService _assetLoader;
        private readonly IGameObjectPoolService _poolService;
        private readonly Dictionary<string, UIView> _viewsByName = new();
        private readonly Dictionary<UIView, IGameObjectPool> _pools = new();
        private readonly List<UIView> _viewList = new();
        private UIView _currentView;
        private bool _isInitialized;

        public UIService(IAssetLoaderService asserLoader, IGameObjectPoolService pooolService)
        {
            _assetLoader = asserLoader;
            _poolService = pooolService;
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
            UIViewBank[] banks = _assetLoader.LoadAssetsFromResources<UIViewBank>("UI");
            foreach (var bank in banks)
            {
                foreach (var view in bank.ViewPrefabs)
                {
                    _viewsByName.Add(view.name, view);
                    _pools.Add(view, _poolService.GetOrCreatePool(view.gameObject));
                }
            }
        }

        public UIView CreateView(string viewName)
        {
            if (!_viewsByName.TryGetValue(viewName, out var view) ||
                !_pools.TryGetValue(view, out var pool))
            {
                throw new Exception($"Unable to instantiate UIView with name: {viewName}");
            }

            return pool.Spawn<UIView>();
        }

        public void RegisterView(UIView view)
        {
            _viewList.Add(view);
            UpdateCurrentView();
        }

        public void UnregisterView(UIView view)
        {
            _viewList.Remove(view);
            if (_pools.TryGetValue(view, out var pool))
            {
                pool.Despawn(view.gameObject);
            }
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