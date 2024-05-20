//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public interface IAssetService
    {
        T LoadAssetFromResources<T>(string path) where T : Object;
        Task<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object;
        T[] LoadAssetsFromResources<T>(string path) where T : Object;
        T FindAssetInResources<T>() where T : Object;
        T[] FindAssetsInResources<T>() where T : Object;
        T Instantiate<T>(T prefab = null, string name = null, Transform parent = null) where T : Component;
    }
}