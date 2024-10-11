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
        private readonly IAssetLoaderService _assetLoader;
        private readonly IGameObjectPoolService _poolService;
        private Dictionary<string, GameObject> _viewPrefabs;
        private readonly List<UIView> _viewList = new();
        private UIView _currentView;
        private bool _isInitialized;

        public UIService(IAssetLoaderService asserLoader, IGameObjectPoolService pool)
        {
            _assetLoader = asserLoader;
            _poolService = pool;
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
            UIViewBank[] banks = _assetLoader.LoadAssetsFromResources<UIViewBank>("UI");
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
                .GetOrCreatePool(prefab)
                .Spawn()
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
            view.gameObject.GetComponent<GameObjectPool.PoolItem>().Despawn();
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