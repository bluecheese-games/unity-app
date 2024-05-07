//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public interface IPoolService
    {
        void Initialize(GameObject prefab, PoolOptions options = default);
        void Initialize<T>(PoolOptions options = default);
        GameObject Spawn(GameObject prefab);
        T Spawn<T>() where T : Component;
        void Despawn(GameObject instance, float delay = 0f);
        void Remove(GameObject instance);
    }

    public struct PoolOptions
    {
        public bool UseContainer;
        public int InitialCapacity;
        public bool DontDestroyOnLoad;
    }

    public interface IRecyclable
    {
        void Recycle();
    }
}
