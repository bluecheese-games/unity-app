//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public interface IPoolService
    {
        /// <summary>
        /// Initializes the pool with the given prefab and options.
        /// </summary>
        void Initialize(GameObject prefab, PoolOptions options = default);

        /// <summary>
        /// Initializes the pool with the given component type and options.
        /// </summary>
        void Initialize<T>(PoolOptions options = default);

        /// <summary>
        /// Spawns an instance of the given prefab.
        /// The instance will be taken from the pool if available, otherwise a new instance will be created.
        /// </summary>
        GameObject Spawn(GameObject prefab);

        /// <summary>
        /// Spawns an instance of the given component type.
        /// The instance will be taken from the pool if available, otherwise a new instance will be created.
        /// </summary>
        T Spawn<T>() where T : Component;

        /// <summary>
        /// Despawns the given instance.
        /// The instance will be returned to the pool.
        /// </summary>
        void Despawn(GameObject instance, float delay = 0f);

        /// <summary>
        /// Removes the given instance from the pool.
        /// </summary>
        void Remove(GameObject instance);
    }

    public struct PoolOptions
    {
        /// <summary>
        /// If true, the pool will use a container object to store the instances.
        /// </summary>
        public bool UseContainer;

        /// <summary>
        /// The initial capacity of the pool.
        /// </summary>
        public int InitialCapacity;

        /// <summary>
        /// If true, the pool will not be destroyed when a new scene is loaded.
        /// </summary>
        public bool DontDestroyOnLoad;

        /// <summary>
        /// The custom factory function to create new instances.
        /// If not set, the pool will use the prefab to instantiate new instances.
        /// </summary>
        public Func<GameObject> Factory;
    }

    public interface IRecyclable
    {
        /// <summary>
        /// This method is called when the instance is recycled from the pool.
        /// </summary>
        void Recycle();
    }
}
