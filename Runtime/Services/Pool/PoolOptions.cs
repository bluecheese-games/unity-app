//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
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
}
