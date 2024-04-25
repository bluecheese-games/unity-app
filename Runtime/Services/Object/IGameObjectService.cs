//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public interface IGameObjectService
    {
        GameObject CreateEmptyObject();
        T CreateObject<T>() where T : Component;
        void Destroy(GameObject obj, float delay = 0);
        GameObject Instantiate(GameObject prefab);
        T Instantiate<T>(T prefab) where T : Component;
        T Instantiate<T>(GameObject prefab) where T : Component;
    }
}