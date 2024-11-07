//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
	public struct PoolOptions
    {
        public static PoolOptions Default = new()
        {
            UseContainer = true,
            FillAmount = 0,
            Capacity = GameObjectPool.DefaultCapacity,
            Overflow = PoolOverflow.LogError,
            DontDestroyOnLoad = false,
            Factory = null
        };

        /// <summary>
        /// If true, the pool will use a container object to store the instances of this pool.
        /// </summary>
        public bool UseContainer;

        /// <summary>
        /// Automarically fills the pool with instances.
        /// </summary>
        public int FillAmount;

        /// <summary>
        /// The capacity of the pool.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// The policy to handle the overflow of the pool.
        /// </summary>
        public PoolOverflow Overflow;

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

	public enum PoolOverflow
    {
        /// <summary>
        /// Ignore the pool capacity and returns a new instance anyway.
        /// </summary>
        Force,

        /// <summary>
        /// Returns a new instance and logs an error.
        /// </summary>
		LogError,

		/// <summary>
		/// Recycles the oldest active instance.
		/// </summary>
		RecycleActive,

		/// <summary>
		/// Cancel the spawn and returns null.
		/// </summary>
		ReturnsNull
	}
}
