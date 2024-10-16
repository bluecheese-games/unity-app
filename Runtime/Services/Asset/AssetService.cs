//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
    public class AssetService : IAssetLoaderService, IAssetFinderService
    {
        public T LoadAssetFromResources<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public async UniTask<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            await request;
            return (T)request.asset;
        }

        public T[] LoadAssetsFromResources<T>(string path) where T : Object
        {
            return Resources.LoadAll<T>(path);
        }

        public T FindAssetInResources<T>() where T : Object
        {
            var assets = Resources.FindObjectsOfTypeAll<T>();
            if (assets.Length > 0) return assets[0];
            return null;
        }

        public T[] FindAssetsInResources<T>() where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>();
        }
    }
}