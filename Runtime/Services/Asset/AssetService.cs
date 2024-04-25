//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;
using Core.Utils;

namespace BlueCheese.Unity.App.Services
{
    public class AssetService : IAssetService
    {
        public T LoadAssetFromResources<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public async Task<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            await request;
            return (T)request.asset;
        }

        public T[] LoadAssetsFromResources<T>(string path) where T : Object
        {
            return Resources.LoadAll<T>(path);
        }

        public T Instantiate<T>(T prefab = null, string name = null, Transform parent = null) where T : Component
        {
            T instance = prefab != null ? Object.Instantiate(prefab, parent) : new GameObject().AddComponent<T>();

            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }

            return instance;
        }
    }
}