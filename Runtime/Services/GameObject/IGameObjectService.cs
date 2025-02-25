//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
    public interface IGameObjectService
    {
        GameObject CreateEmptyObject(string name = null);
        T CreateObject<T>() where T : Component;
        void Destroy(GameObject obj, float delay = 0);
        GameObject Instantiate(GameObject prefab);
		UniTask<GameObject> InstantiateAsync(GameObject prefab);
        T Instantiate<T>(T prefab) where T : Component;
		UniTask<T> InstantiateAsync<T>(T prefab) where T : Component;
        T Instantiate<T>(GameObject prefab) where T : Component;
		UniTask<T> InstantiateAsync<T>(GameObject prefab) where T : Component;
        void DontDestroyOnLoad(GameObject obj);
        T Find<T>(bool includeInactive = false) where T : Component;
        T[] FindAll<T>(bool includeInactive = false) where T : Component;
    }
}